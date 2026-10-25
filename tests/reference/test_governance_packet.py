"""Synthetic governance packet contract tests; no live records or providers."""
import unittest
from dataclasses import replace
from datetime import UTC, datetime, timedelta

from reference.governance_packet import (
    ActionRecord,
    AgendaItem,
    DecisionRecord,
    GovernancePacket,
    IdempotencyRecord,
    MinutesRevision,
    OptionalSnapshot,
    ProviderStatus,
    classify_retry,
    evaluate_optional_snapshot,
    select_minutes_revision,
    validate_packet,
)


class GovernancePacketTests(unittest.TestCase):
    def setUp(self) -> None:
        self.minutes = MinutesRevision("minutes-syn-1", "meeting-syn-1", 1, "a" * 64, False)
        self.agenda = AgendaItem("agenda-syn-1", "meeting-syn-1", 1)
        self.decision = DecisionRecord("decision-syn-1", "agenda-syn-1", "minutes-syn-1", 1)
        self.action = ActionRecord("action-syn-1", "agenda-syn-1", "decision-syn-1")
        self.packet = GovernancePacket(
            "meeting-syn-1", (self.agenda,), (self.minutes,), (self.decision,), (self.action,)
        )
        self.now = datetime(2026, 10, 25, 12, tzinfo=UTC)
        self.snapshot = OptionalSnapshot(
            "provider-syn", "1", "snapshot-syn", "meeting-syn-1",
            self.now - timedelta(minutes=2), "correlation-syn", "retry-syn-1",
        )

    def test_valid_packet_preserves_each_record_identity(self) -> None:
        validate_packet(self.packet)
        self.assertEqual(self.packet.decisions[0].minutes_id, self.packet.minutes[0].minutes_id)
        self.assertEqual(self.packet.actions[0].decision_id, self.packet.decisions[0].decision_id)

    def test_agenda_identity_revision_and_duplicate_are_checked(self) -> None:
        for bad in (
            replace(self.agenda, item_id=" "),
            replace(self.agenda, meeting_id="other"),
            replace(self.agenda, revision=0),
        ):
            with self.assertRaises(ValueError):
                validate_packet(replace(self.packet, agenda_items=(bad,)))
        with self.assertRaisesRegex(ValueError, "duplicate agenda"):
            validate_packet(replace(self.packet, agenda_items=(self.agenda, self.agenda)))
        with self.assertRaisesRegex(ValueError, "meeting_id"):
            validate_packet(replace(self.packet, meeting_id=" "))

    def test_minutes_identity_hash_revision_and_duplicate_are_checked(self) -> None:
        for bad in (
            replace(self.minutes, minutes_id=" "),
            replace(self.minutes, content_hash=""),
            replace(self.minutes, meeting_id="other"),
            replace(self.minutes, revision=0),
        ):
            with self.assertRaises(ValueError):
                validate_packet(replace(self.packet, minutes=(bad,)))
        with self.assertRaisesRegex(ValueError, "duplicate minutes"):
            validate_packet(replace(self.packet, minutes=(self.minutes, self.minutes)))

    def test_decision_requires_local_agenda_and_minutes_revision(self) -> None:
        for bad in (
            replace(self.decision, decision_id=" "),
            replace(self.decision, revision=0),
            replace(self.decision, agenda_item_id="missing"),
            replace(self.decision, minutes_revision=2),
        ):
            with self.assertRaises(ValueError):
                validate_packet(replace(self.packet, decisions=(bad,)))
        with self.assertRaisesRegex(ValueError, "duplicate decision"):
            validate_packet(replace(self.packet, decisions=(self.decision, self.decision)))

    def test_action_requires_local_links_and_matching_decision_agenda(self) -> None:
        for bad in (
            replace(self.action, action_id=" "),
            replace(self.action, revision=0),
            replace(self.action, agenda_item_id="missing"),
            replace(self.action, decision_id="missing"),
            replace(self.action, agenda_item_id="other"),
        ):
            with self.assertRaises(ValueError):
                validate_packet(replace(self.packet, actions=(bad,)))
        with self.assertRaisesRegex(ValueError, "duplicate action"):
            validate_packet(replace(self.packet, actions=(self.action, self.action)))
        validate_packet(replace(self.packet, actions=(ActionRecord("action-syn-2", "agenda-syn-1"),)))
        other_agenda = AgendaItem("agenda-syn-2", "meeting-syn-1", 2)
        other_action = ActionRecord("action-syn-3", other_agenda.item_id, self.decision.decision_id)
        with self.assertRaisesRegex(ValueError, "agenda identity"):
            validate_packet(replace(
                self.packet, agenda_items=(self.agenda, other_agenda), actions=(other_action,)
            ))

    def test_minutes_preview_preserves_human_edits(self) -> None:
        edited = replace(self.minutes, content_hash="b" * 64, human_edited=True)
        candidate = replace(self.minutes, revision=2, content_hash="c" * 64)
        self.assertEqual(select_minutes_revision(None, candidate), (candidate, "candidate-selected"))
        self.assertEqual(select_minutes_revision(edited, candidate), (edited, "human-edit-preserved"))
        self.assertEqual(select_minutes_revision(self.minutes, candidate), (candidate, "candidate-selected"))
        with self.assertRaisesRegex(ValueError, "identity"):
            select_minutes_revision(self.minutes, replace(candidate, meeting_id="other"))
        with self.assertRaisesRegex(ValueError, "newer"):
            select_minutes_revision(self.minutes, self.minutes)

    def evaluate_snapshot(
        self, snapshot: OptionalSnapshot | None, now: datetime, max_age: timedelta,
        meeting_id: str = "meeting-syn-1", contract_version: str = "1",
    ) -> ProviderStatus:
        return evaluate_optional_snapshot(
            snapshot, expected_meeting_id=meeting_id,
            expected_contract_version=contract_version, now=now, max_age=max_age,
        )

    def test_optional_snapshot_absence_missing_incompatible_stale_and_ready(self) -> None:
        max_age = timedelta(minutes=10)
        self.assertEqual(self.evaluate_snapshot(None, self.now, max_age), ProviderStatus.ABSENT)
        self.assertEqual(
            self.evaluate_snapshot(replace(self.snapshot, snapshot_id=" "), self.now, max_age),
            ProviderStatus.MISSING,
        )
        self.assertEqual(
            self.evaluate_snapshot(replace(self.snapshot, contract_version="2"), self.now, max_age),
            ProviderStatus.INCOMPATIBLE,
        )
        self.assertEqual(
            self.evaluate_snapshot(replace(self.snapshot, meeting_id="other"), self.now, max_age),
            ProviderStatus.INCOMPATIBLE,
        )
        self.assertEqual(
            self.evaluate_snapshot(replace(self.snapshot, captured_at=self.now - timedelta(days=1)), self.now, max_age),
            ProviderStatus.STALE,
        )
        self.assertEqual(
            self.evaluate_snapshot(replace(self.snapshot, captured_at=self.now + timedelta(seconds=1)), self.now, max_age),
            ProviderStatus.STALE,
        )
        self.assertEqual(
            self.evaluate_snapshot(replace(self.snapshot, captured_at=datetime(2026, 10, 25)), self.now, max_age),
            ProviderStatus.STALE,
        )
        self.assertEqual(self.evaluate_snapshot(self.snapshot, self.now, max_age), ProviderStatus.READY)
        with self.assertRaisesRegex(ValueError, "timezone-aware"):
            self.evaluate_snapshot(self.snapshot, datetime(2026, 10, 25), max_age)
        with self.assertRaisesRegex(ValueError, "positive"):
            self.evaluate_snapshot(self.snapshot, self.now, timedelta(0))
        with self.assertRaisesRegex(ValueError, "expected meeting"):
            self.evaluate_snapshot(self.snapshot, self.now, max_age, meeting_id=" ")
        with self.assertRaisesRegex(ValueError, "expected contract"):
            self.evaluate_snapshot(self.snapshot, self.now, max_age, contract_version=" ")

    def test_retry_classification_is_idempotent_and_conflicts_fail(self) -> None:
        record = IdempotencyRecord("retry-syn-1", "digest-syn-1")
        self.assertEqual(classify_retry((), key="retry-syn-1", payload_hash="digest-syn-1"), "new")
        self.assertEqual(classify_retry((record,), key=record.key, payload_hash=record.payload_hash), "duplicate")
        with self.assertRaisesRegex(ValueError, "different content"):
            classify_retry((record,), key=record.key, payload_hash="other-digest")
        with self.assertRaisesRegex(ValueError, "duplicate keys"):
            classify_retry((record, record), key=record.key, payload_hash=record.payload_hash)
        with self.assertRaisesRegex(ValueError, "idempotency key"):
            classify_retry((), key=" ", payload_hash="digest")
        with self.assertRaisesRegex(ValueError, "payload hash"):
            classify_retry((), key="key", payload_hash=" ")


if __name__ == "__main__":
    unittest.main()
