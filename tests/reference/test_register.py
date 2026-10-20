"""Synthetic register tests: no personal or live credentialing records."""
import unittest
from dataclasses import replace
from datetime import UTC, datetime, timedelta
from typing import cast

from reference.credentialing_register import (
    DecisionState,
    Register,
    Revision,
    Viewer,
    ViewRole,
    project,
)


class RegisterTest(unittest.TestCase):
    def setUp(self) -> None:
        self.at = datetime(2026, 9, 22, tzinfo=UTC)
        self.before = self.at - timedelta(days=1)
        self.after = self.at + timedelta(days=1)
        self.revision = Revision("r1", "d1", "p1", "medicine", "site-1", "scope-1",
                                 DecisionState.GRANTED, self.before, self.after,
                                 self.before, self.before, "decision-1", "authority-1",
                                 ("named-supervisor-required",), "supervision-plan-1", human_confirmed=True)
        self.viewer = Viewer("workforce-user", ViewRole.OPERATIONAL,
                             frozenset({"site-1"}), frozenset({"medicine"}))
        self.register = Register().append(self.revision, expected_version=0)

    def rows(self, register: Register | None = None, viewer: Viewer | None = None) -> tuple[str, ...]:
        return tuple(row.status for row in project(register or self.register, viewer or self.viewer,
                                                   self.at, current_until=self.after).rows)

    def test_recorded_scope_never_asserts_release_to_practise(self) -> None:
        self.assertEqual(self.rows(), ("scope-recorded",))
        row = project(self.register, self.viewer, self.at, current_until=self.after).rows[0]
        self.assertEqual(row.conditions, ("named-supervisor-required",))
        self.assertEqual(row.supervision, "supervision-plan-1")
        self.assertEqual(row.decision_reference, "")
        self.assertEqual(row.authority_reference, "")
        self.assertFalse(hasattr(row, "approved_to_practise"))

    def test_replay_is_idempotent_but_divergent_reuse_is_rejected(self) -> None:
        self.assertIs(self.register.append(self.revision, expected_version=0), self.register)
        with self.assertRaises(ValueError):
            self.register.append(replace(self.revision, scope="changed"), expected_version=1)

    def test_concurrent_append_requires_reconciliation(self) -> None:
        with self.assertRaises(ValueError):
            self.register.append(replace(self.revision, record_id="r2", decision_id="d2"), expected_version=0)

    def test_explicit_supersession_preserves_history(self) -> None:
        next_revision = replace(self.revision, decision_id="d2", supersedes="d1", state=DecisionState.SUSPENDED)
        updated = self.register.append(next_revision, expected_version=1)
        self.assertEqual(len(updated.history), 2)
        self.assertEqual(self.rows(updated), ("suspended",))
        self.assertEqual(self.rows(), ("scope-recorded",))
        with self.assertRaises(ValueError):
            self.register.append(replace(next_revision, supersedes=""), expected_version=1)
        with self.assertRaises(ValueError):
            Register().append(next_revision, expected_version=0)

    def test_future_revisions_do_not_activate_early(self) -> None:
        scheduled = replace(self.revision, decision_id="d2", supersedes="d1", state=DecisionState.REVOKED, effective_at=self.after)
        pending = self.register.append(scheduled, expected_version=1)
        self.assertEqual(self.rows(pending), ("scope-recorded",))
        self.assertEqual(project(pending, self.viewer, self.after, current_until=self.after + timedelta(days=1)).rows[0].status, "revoked")
        not_recorded = Register().append(replace(self.revision, recorded_at=self.after), expected_version=0)
        self.assertEqual(self.rows(not_recorded), ())

    def test_identity_and_chronology_cannot_be_rewritten(self) -> None:
        changed = replace(self.revision, decision_id="d2", supersedes="d1")
        for revision in [replace(changed, practitioner="p2"), replace(changed, profession="nursing"),
                         replace(changed, facility="site-2"), replace(changed, recorded_at=self.before - timedelta(days=1))]:
            with self.assertRaises(ValueError):
                self.register.append(revision, expected_version=1)

    def test_unverified_or_malformed_input_is_not_registered(self) -> None:
        for revision in [replace(self.revision, human_confirmed=False), replace(self.revision, authority_reference=""),
                         replace(self.revision, state=cast(DecisionState, "green")),
                         replace(self.revision, valid_until=self.before), replace(self.revision, recorded_at=self.at.replace(tzinfo=None))]:
            with self.assertRaises(ValueError):
                Register().append(revision, expected_version=0)

    def test_profession_and_facility_permissions_are_enforced(self) -> None:
        for viewer in [replace(self.viewer, facilities=frozenset({"site-2"})), replace(self.viewer, professions=frozenset({"nursing"}))]:
            self.assertEqual(self.rows(viewer=viewer), ())
        nursing = replace(self.revision, record_id="r2", decision_id="d2", profession="nursing")
        register = self.register.append(nursing, expected_version=1)
        self.assertEqual(len(self.rows(register, replace(self.viewer, professions=frozenset({"medicine", "nursing"})))), 2)

    def test_practitioner_only_sees_own_record(self) -> None:
        viewer = replace(self.viewer, role=ViewRole.PRACTITIONER, practitioner="other")
        self.assertEqual(self.rows(viewer=viewer), ())
        viewer = replace(viewer, practitioner="p1")
        self.assertEqual(self.rows(viewer=viewer), ("scope-recorded",))
        row = project(self.register, viewer, self.at, current_until=self.after).rows[0]
        self.assertEqual(row.decision_reference, "decision-1")

    def test_detailed_views_remain_within_row_permissions(self) -> None:
        for role in [ViewRole.CREDENTIALING, ViewRole.AUDITOR]:
            viewer = replace(self.viewer, role=role)
            self.assertEqual(project(self.register, viewer, self.at, current_until=self.after).rows[0].authority_reference, "authority-1")
            self.assertEqual(self.rows(viewer=replace(viewer, facilities=frozenset())), ())

    def test_statuses_are_not_rag_scores(self) -> None:
        for state in [DecisionState.REVOKED, DecisionState.REFUSED, DecisionState.SUSPENDED]:
            register = Register().append(replace(self.revision, state=state), expected_version=0)
            self.assertEqual(self.rows(register), (state.value,))
        for start, end, status in [(self.after, self.after + timedelta(days=1), "not-yet-effective"),
                                   (self.before - timedelta(days=1), self.at, "expired")]:
            register = Register().append(replace(self.revision, valid_from=start, valid_until=end), expected_version=0)
            self.assertEqual(self.rows(register), (status,))
        self.assertEqual(project(self.register, self.viewer, self.at, current_until=self.at).rows[0].status, "currentness-unverified")

    def test_unauthenticated_or_unknown_roles_and_naive_query_are_rejected(self) -> None:
        for viewer in [replace(self.viewer, principal=""), replace(self.viewer, role=cast(ViewRole, "admin"))]:
            with self.assertRaises(PermissionError):
                self.rows(viewer=viewer)
        with self.assertRaises(ValueError):
            project(self.register, self.viewer, self.at.replace(tzinfo=None), current_until=self.after)

    def test_empty_and_adverse_views_keep_currentness_explicit(self) -> None:
        empty = project(Register(), self.viewer, self.at, current_until=self.at)
        self.assertEqual(empty.rows, ())
        self.assertFalse(empty.currentness_verified)
        revoked = Register().append(replace(self.revision, state=DecisionState.REVOKED), expected_version=0)
        stale = project(revoked, self.viewer, self.at, current_until=self.at)
        self.assertFalse(stale.currentness_verified)
        self.assertEqual(stale.rows[0].recorded_state, DecisionState.REVOKED)

    def test_scheduled_change_cannot_revive_later_revocation(self) -> None:
        scheduled = replace(self.revision, decision_id="d2", supersedes="d1", effective_at=self.after)
        register = self.register.append(scheduled, expected_version=1)
        revoked = replace(scheduled, decision_id="d3", supersedes="d2", state=DecisionState.REVOKED,
                          effective_at=self.at, recorded_at=self.at)
        register = register.append(revoked, expected_version=2)
        later = self.after + timedelta(hours=1)
        self.assertEqual(project(register, self.viewer, later, current_until=later + timedelta(days=1)).rows[0].status, "revoked")
