"""Real local SQLite tests, using synthetic decisions and no external services."""
import hashlib
import sqlite3
import tempfile
import unittest
from concurrent.futures import ThreadPoolExecutor
from dataclasses import replace
from datetime import UTC, datetime, timedelta
from pathlib import Path
from threading import Event
from typing import cast
from unittest.mock import patch

from reference.credentialing_register import DecisionState, Register, Revision, Viewer, ViewRole
from reference.register_codec import encode_revision
from reference.register_store import EMPTY_DIGEST, SQLiteRegisterStore


def decision(number: int = 1) -> Revision:
    start = datetime(2026, 1, 1, tzinfo=UTC)
    return Revision(
        record_id="synthetic-record", decision_id=f"synthetic-decision-{number}",
        practitioner="synthetic-practitioner", profession="dental", facility="synthetic-site",
        scope="synthetic-scope", state=DecisionState.GRANTED,
        valid_from=start, valid_until=start + timedelta(days=30),
        effective_at=start, recorded_at=start + timedelta(seconds=number),
        decision_reference="synthetic-decision-receipt", authority_reference="synthetic-authority",
        conditions=("Synthetic limitation",), supervision="Synthetic supervision",
        supersedes=f"synthetic-decision-{number - 1}" if number > 1 else "",
        human_confirmed=True,
    )


class StoreTests(unittest.TestCase):
    def setUp(self) -> None:
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.path = Path(directory.name) / "synthetic-register.sqlite"
        self.store = SQLiteRegisterStore.initialize(self.path)

    def raw(self) -> sqlite3.Connection:
        connection = sqlite3.connect(self.path, autocommit=True, timeout=0)
        self.addCleanup(connection.close)
        return connection

    def viewer(self, role: ViewRole = ViewRole.OPERATIONAL) -> Viewer:
        return Viewer("synthetic-user", role, frozenset({"synthetic-site"}),
                      frozenset({"dental"}), "synthetic-practitioner")

    def test_restart_preserves_complete_history(self) -> None:
        self.assertEqual(self.store.load().history, ())
        self.store.append(decision(), expected_version=0)
        expected = self.store.append(replace(decision(2), state=DecisionState.SUSPENDED),
                                     expected_version=1)
        reopened = SQLiteRegisterStore(self.path)
        self.assertEqual(reopened.load(), expected)
        self.assertEqual(len(expected.history), 2)
        self.assertEqual(expected.history[-1].supervision, "Synthetic supervision")

    def test_missing_database_does_not_become_empty_register(self) -> None:
        missing = self.path.with_name("missing.sqlite")
        with self.assertRaises(sqlite3.OperationalError):
            SQLiteRegisterStore(missing).load()
        self.assertFalse(missing.exists())

    def test_initialization_never_overwrites_existing_file(self) -> None:
        self.store.append(decision(), expected_version=0)
        with self.assertRaises(FileExistsError):
            SQLiteRegisterStore.initialize(self.path)
        self.assertEqual(self.store.load().history, (decision(),))

    def test_exact_replay_after_restart_does_not_duplicate(self) -> None:
        expected = self.store.append(decision(), expected_version=0)
        self.assertEqual(SQLiteRegisterStore(self.path).append(decision(), expected_version=0),
                         expected)
        self.assertEqual(self.store.load(), expected)

    def test_changed_replay_is_rejected(self) -> None:
        self.store.append(decision(), expected_version=0)
        with self.assertRaisesRegex(ValueError, "reused"):
            self.store.append(replace(decision(), scope="different"), expected_version=1)
        self.assertEqual(self.store.load().history, (decision(),))

    def test_two_writers_must_reconcile_stale_version(self) -> None:
        second = SQLiteRegisterStore(self.path)
        old_version = len(second.load().history)
        self.store.append(decision(), expected_version=0)
        with self.assertRaisesRegex(ValueError, "Concurrent"):
            second.append(decision(2), expected_version=old_version)
        self.assertEqual(len(second.append(decision(2), expected_version=1).history), 2)

    def test_expected_version_is_not_coerced(self) -> None:
        for version in (True, 0.0, "0", -1):
            with self.subTest(version=version), self.assertRaises(ValueError):
                self.store.append(decision(), expected_version=cast(int, version))
        self.assertEqual(self.store.load().history, ())

    def test_competing_write_lock_is_visible_and_preserves_data(self) -> None:
        blocker = self.raw()
        blocker.execute("BEGIN IMMEDIATE")
        with self.assertRaises(sqlite3.OperationalError):
            self.store.append(decision(), expected_version=0)
        blocker.execute("ROLLBACK")
        self.assertEqual(self.store.load().history, ())
        self.store.append(decision(), expected_version=0)

    def test_checkpoint_failure_rolls_back_event_insert(self) -> None:
        connection = self.raw()
        connection.execute("CREATE TRIGGER synthetic_failure BEFORE UPDATE ON register_meta "
                           "BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END")
        with self.assertRaises(sqlite3.IntegrityError):
            self.store.append(decision(), expected_version=0)
        self.assertEqual(self.store.load().history, ())
        connection.execute("DROP TRIGGER synthetic_failure")
        self.store.append(decision(), expected_version=0)

    def test_uncommitted_interrupted_connection_cannot_remove_history(self) -> None:
        self.store.append(decision(), expected_version=0)
        interrupted = self.raw()
        interrupted.execute("BEGIN IMMEDIATE")
        interrupted.execute("DELETE FROM register_events")
        interrupted.close()
        self.assertEqual(SQLiteRegisterStore(self.path).load().history, (decision(),))

    def test_payload_mutation_detected_without_repair(self) -> None:
        self.store.append(decision(), expected_version=0)
        self.raw().execute("UPDATE register_events SET payload=?", ("synthetic corruption",))
        with self.assertRaisesRegex(ValueError, "integrity"):
            self.store.load()
        with self.assertRaises(ValueError):
            self.store.append(decision(2), expected_version=1)

    def test_missing_terminal_event_detected(self) -> None:
        self.store.append(decision(), expected_version=0)
        self.raw().execute("DELETE FROM register_events")
        with self.assertRaisesRegex(ValueError, "incomplete"):
            self.store.load()

    def test_sequence_gap_and_identifier_mismatch_detected(self) -> None:
        self.store.append(decision(), expected_version=0)
        connection = self.raw()
        connection.execute("UPDATE register_events SET sequence=2")
        with self.assertRaisesRegex(ValueError, "sequence"):
            self.store.load()
        connection.execute("UPDATE register_events SET sequence=1, decision_id='wrong'")
        with self.assertRaisesRegex(ValueError, "identifier"):
            self.store.load()

    def test_unknown_schema_and_missing_checkpoint_fail(self) -> None:
        connection = self.raw()
        connection.execute("UPDATE register_meta SET schema_version=2")
        with self.assertRaises(ValueError):
            self.store.load()
        connection.execute("DELETE FROM register_meta")
        with self.assertRaises(ValueError):
            self.store.load()

    def test_incompatible_file_is_not_migrated(self) -> None:
        incompatible = self.path.with_name("uninitialised.sqlite")
        incompatible.touch()
        with self.assertRaises(sqlite3.OperationalError):
            SQLiteRegisterStore(incompatible).load()
        self.assertEqual(incompatible.stat().st_size, 0)

    def test_invalid_stored_types_and_duplicate_history_are_rejected(self) -> None:
        connection = self.raw()
        connection.execute("DROP TABLE register_events")
        connection.execute("CREATE TABLE register_events(sequence, decision_id, payload, digest)")
        connection.execute("INSERT INTO register_events VALUES(1, 'synthetic', NULL, '')")
        with self.assertRaisesRegex(ValueError, "payload"):
            self.store.load()
        connection.execute("DELETE FROM register_events")
        payload = encode_revision(decision())
        first_hash = hashlib.sha256((EMPTY_DIGEST + payload).encode()).hexdigest()
        second_hash = hashlib.sha256((first_hash + payload).encode()).hexdigest()
        connection.executemany("INSERT INTO register_events VALUES(?, ?, ?, ?)",
                               [(1, decision().decision_id, payload, first_hash),
                                (2, decision().decision_id, payload, second_hash)])
        with self.assertRaisesRegex(ValueError, "Duplicate"):
            self.store.load()

    def test_invalid_human_decision_cannot_be_persisted(self) -> None:
        with self.assertRaises(ValueError):
            self.store.append(replace(decision(), human_confirmed=False), expected_version=0)
        self.assertEqual(self.store.load().history, ())

    def test_views_retain_access_and_currentness_boundaries(self) -> None:
        self.store.append(decision(), expected_version=0)
        at = decision().recorded_at
        lease = at + timedelta(minutes=10)
        operational = self.store.view(self.viewer(), at, current_until=lease)
        self.assertEqual(operational.rows[0].decision_reference, "")
        self.assertEqual(operational.rows[0].conditions, decision().conditions)
        detailed = self.store.view(self.viewer(ViewRole.CREDENTIALING), at, current_until=lease)
        self.assertEqual(detailed.rows[0].decision_reference, decision().decision_reference)
        for viewer in (replace(self.viewer(), professions=frozenset({"medical"})),
                       replace(self.viewer(), facilities=frozenset()),
                       replace(self.viewer(ViewRole.PRACTITIONER), practitioner="other")):
            self.assertEqual(self.store.view(viewer, at, current_until=lease).rows, ())
        stale = self.store.view(self.viewer(), lease, current_until=lease)
        self.assertFalse(stale.currentness_verified)
        self.assertEqual(stale.rows[0].status, "currentness-unverified")
        self.assertEqual(stale.rows[0].recorded_state, DecisionState.GRANTED)

    def test_sql_metacharacters_remain_values(self) -> None:
        value = replace(decision(), scope="synthetic '); DROP TABLE register_meta; --")
        self.store.append(value, expected_version=0)
        self.assertEqual(self.store.load().history, (value,))

    def test_unsafe_supersession_does_not_change_storage(self) -> None:
        self.store.append(decision(), expected_version=0)
        with self.assertRaisesRegex(ValueError, "supersession"):
            self.store.append(replace(decision(2), supersedes=""), expected_version=1)
        self.assertEqual(self.store.load().history, (decision(),))

    def test_failed_commit_rolls_back_and_allows_clean_retry(self) -> None:
        reader = self.raw()
        reader.execute("BEGIN")
        reader.execute("SELECT * FROM register_meta").fetchall()
        with self.assertRaises(sqlite3.OperationalError):
            self.store.append(decision(), expected_version=0)
        reader.execute("ROLLBACK")
        self.assertEqual(self.store.load().history, ())
        self.store.append(decision(), expected_version=0)

    def test_old_replay_cannot_undo_later_revocation(self) -> None:
        self.store.append(decision(), expected_version=0)
        revoked = replace(decision(2), state=DecisionState.REVOKED)
        self.store.append(revoked, expected_version=1)
        reopened = SQLiteRegisterStore(self.path)
        self.assertEqual(len(reopened.append(decision(), expected_version=0).history), 2)
        at = revoked.recorded_at
        view = reopened.view(self.viewer(), at, current_until=at + timedelta(minutes=1))
        self.assertEqual(view.rows[0].recorded_state, DecisionState.REVOKED)
        self.assertEqual(view.rows[0].status, "revoked")

    def test_simultaneous_writers_cannot_lose_an_update(self) -> None:
        acquired = Event()
        competitor_closed = Event()
        read = SQLiteRegisterStore._read
        first = replace(decision(1), record_id="synthetic-record-1", supersedes="")
        second = replace(decision(2), record_id="synthetic-record-2", supersedes="")

        def hold_snapshot(connection: sqlite3.Connection) -> tuple[Register, str]:
            snapshot = read(connection)
            acquired.set()
            self.assertTrue(competitor_closed.wait(timeout=5), "Competing connection did not close")
            return snapshot

        def winner() -> str:
            self.store.append(first, expected_version=0)
            return "committed"

        def competitor() -> str:
            self.assertTrue(acquired.wait(timeout=5), "First write did not acquire its transaction")
            try:
                with self.assertRaises(sqlite3.OperationalError) as raised:
                    SQLiteRegisterStore(self.path).append(second, expected_version=0)
                self.assertIn(raised.exception.sqlite_errorcode & 255,
                              (sqlite3.SQLITE_BUSY, sqlite3.SQLITE_LOCKED))
                return "reconcile"
            finally:
                competitor_closed.set()

        # Control only scheduling: both connections and all reads/writes remain real.
        # A free-running zero-timeout race may reject BOTH contenders; that is not data loss.
        with patch.object(SQLiteRegisterStore, "_read", side_effect=hold_snapshot):
            with ThreadPoolExecutor(max_workers=2) as workers:
                pending = (workers.submit(winner), workers.submit(competitor))
                results = [future.result() for future in pending]
        self.assertCountEqual(results, ["committed", "reconcile"])
        self.assertEqual(self.store.load().history, (first,))
        self.assertEqual(self.store.append(second, expected_version=1).history, (first, second))
