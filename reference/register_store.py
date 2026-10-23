"""Transactional local persistence for synthetic register qualification.

This is not a production database selection or an authenticated service. Only a
protected host may provide verified decisions, viewer grants and currentness.
"""
import hashlib
import os
import sqlite3
from collections.abc import Iterator
from contextlib import closing, contextmanager
from datetime import datetime
from pathlib import Path
from typing import cast

from reference.credentialing_register import Register, RegisterView, Revision, Viewer, project
from reference.register_codec import decode_revision, encode_revision

EMPTY_DIGEST = "0" * 64


def _digest(previous: str, payload: str) -> str:
    return hashlib.sha256((previous + payload).encode("utf-8")).hexdigest()


class SQLiteRegisterStore:
    """Per-operation connections; explicit creation, no destructive reinitialization.

    The path is host configuration in a protected directory, not user input.
    Opening a missing database fails rather than presenting an empty register.
    """

    def __init__(self, path: Path) -> None:
        self._uri = path.resolve().as_uri() + "?mode=rw"

    @classmethod
    def initialize(cls, path: Path) -> SQLiteRegisterStore:
        # Exclusive creation preserves any existing database, including symlinks.
        descriptor = os.open(path, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
        os.close(descriptor)
        store = cls(path)
        with store._transaction(write=True) as connection:
            connection.execute(
                "CREATE TABLE register_meta (singleton INTEGER PRIMARY KEY CHECK(singleton=1), "
                "schema_version INTEGER NOT NULL, revision_count INTEGER NOT NULL, "
                "head_hash TEXT NOT NULL) STRICT"
            )
            connection.execute(
                "CREATE TABLE register_events (sequence INTEGER PRIMARY KEY, "
                "decision_id TEXT NOT NULL UNIQUE, payload TEXT NOT NULL, digest TEXT NOT NULL) STRICT"
            )
            connection.execute("INSERT INTO register_meta VALUES (1, 1, 0, ?)", (EMPTY_DIGEST,))
        return store

    @contextmanager
    def _transaction(self, *, write: bool) -> Iterator[sqlite3.Connection]:
        with closing(sqlite3.connect(self._uri, uri=True, autocommit=True, timeout=0)) as connection:
            connection.execute("PRAGMA synchronous=FULL")
            connection.execute("BEGIN IMMEDIATE" if write else "BEGIN")
            try:
                yield connection
                connection.execute("COMMIT")
            finally:
                if connection.in_transaction:
                    connection.execute("ROLLBACK")

    @staticmethod
    def _read(connection: sqlite3.Connection) -> tuple[Register, str]:
        rows = cast(list[tuple[int, str, str, str]], connection.execute(
            "SELECT sequence, decision_id, payload, digest FROM register_events ORDER BY sequence"
        ).fetchall())
        register = Register()
        head = EMPTY_DIGEST
        for expected, (sequence, decision_id, payload, digest) in enumerate(rows, start=1):
            if not isinstance(payload, str) or sequence != expected:
                raise ValueError("Invalid stored register sequence or payload")
            if _digest(head, payload) != digest:
                raise ValueError("Register integrity check failed")
            revision = decode_revision(payload)
            if revision.decision_id != decision_id:
                raise ValueError("Stored decision identifier mismatch")
            register = register.append(revision, expected_version=expected - 1)
            if len(register.history) != expected:
                raise ValueError("Duplicate stored decision")
            head = digest
        metadata = connection.execute(
            "SELECT schema_version, revision_count, head_hash FROM register_meta ORDER BY singleton"
        ).fetchall()
        if metadata != [(1, len(rows), head)]:
            raise ValueError("Unsupported schema or incomplete register history")
        return register, head

    def load(self) -> Register:
        """Read one validated snapshot; raw history is for the trusted host only."""
        with self._transaction(write=False) as connection:
            register, _ = self._read(connection)
            return register

    def append(self, revision: Revision, *, expected_version: int) -> Register:
        """Atomically compare, append and checkpoint one verified human decision.

        Exact replays are harmless, including after a lost response. Concurrent
        changes fail visibly. No automatic retries, notifications or side effects.
        """
        if type(expected_version) is not int or expected_version < 0:
            raise ValueError("Nonnegative integer expected version required")
        payload = encode_revision(revision)
        normalized = decode_revision(payload)
        with self._transaction(write=True) as connection:
            before, head = self._read(connection)
            after = before.append(normalized, expected_version=expected_version)
            if after == before:
                return before
            head = _digest(head, payload)
            connection.execute("INSERT INTO register_events VALUES (?, ?, ?, ?)",
                               (len(after.history), normalized.decision_id, payload, head))
            connection.execute(
                "UPDATE register_meta SET revision_count=?, head_hash=? WHERE singleton=1",
                (len(after.history), head),
            )
            return after

    def view(self, viewer: Viewer, at: datetime, *, current_until: datetime) -> RegisterView:
        """Reuse role-scoped projection; successful storage reads do not grant freshness."""
        return project(self.load(), viewer, at, current_until=current_until)
