"""Synthetic authority tests; no real person or live delegation is configured."""
import unittest
from dataclasses import replace
from datetime import UTC, datetime, timedelta
from zoneinfo import ZoneInfo

from reference.delegations import Context, Grant, Profile, check_authority


class DelegationTest(unittest.TestCase):
    def setUp(self) -> None:
        self.at = datetime(2026, 9, 22, tzinfo=UTC)
        self.start = self.at - timedelta(days=1)
        self.end = self.at + timedelta(days=1)
        self.grant = Grant("g1", "ordinary-scope", "medical-delegate", frozenset({"medicine"}),
                           frozenset({"site-1"}), frozenset({"renewal"}), frozenset({"recommendation"}))
        self.profile = Profile("synthetic", "1", "instrument-1", "approval-1", "editor", "approver",
                               self.start, self.end, (self.grant,), True, True)
        self.context = Context("delegate", "applicant", "medical-delegate", "occupancy-1",
                               self.start, self.end, "ordinary-scope", "medicine", "site-1", "renewal",
                               frozenset({"recommendation"}), conflict=False, participated_in_committee=False)

    def test_authority_is_specific_and_not_credentialing_approval(self) -> None:
        result = check_authority(self.profile, self.context, self.at)
        self.assertTrue(result.permitted)
        self.assertEqual(result.grant_reference, "g1")
        self.assertEqual(result.profile_revision, "1")
        self.assertFalse(hasattr(result, "scope_approved"))

    def test_draft_and_unverified_profiles_fail_closed(self) -> None:
        for profile in [replace(self.profile, active=False), replace(self.profile, controlled_source_verified=False)]:
            result = check_authority(profile, self.context, self.at)
            self.assertEqual(result.reason, "unverified-or-inactive-profile")

    def test_each_required_evidence_field_is_checked(self) -> None:
        for profile in [
            replace(self.profile, identifier=" "), replace(self.profile, revision=" "),
            replace(self.profile, instrument_reference=" "), replace(self.profile, approval_reference=" "),
            replace(self.profile, edited_by=" "), replace(self.profile, approved_by=" "),
        ]:
            result = check_authority(profile, self.context, self.at)
            self.assertEqual(result.reason, "missing-authority-evidence")
        for context in [
            replace(self.context, actor=" "), replace(self.context, practitioner=" "),
            replace(self.context, role=" "), replace(self.context, occupancy_reference=" "),
            replace(self.context, function=" "), replace(self.context, profession=" "),
            replace(self.context, facility=" "), replace(self.context, pathway=" "),
        ]:
            result = check_authority(self.profile, context, self.at)
            self.assertEqual(result.reason, "missing-authority-evidence")

    def test_self_approval_and_self_decision_are_blocked(self) -> None:
        result = check_authority(replace(self.profile, approved_by="editor"), self.context, self.at)
        self.assertEqual(result.reason, "configuration-self-approval")
        for context in [replace(self.context, actor="applicant"), replace(self.context, conflict=True), replace(self.context, conflict=None)]:
            self.assertEqual(check_authority(self.profile, context, self.at).reason, "self-decision-or-conflict")

    def test_effective_intervals_are_half_open(self) -> None:
        for at in [self.start - timedelta(seconds=1), self.end]:
            self.assertEqual(check_authority(self.profile, self.context, at).reason, "profile-outside-effective-period")
        self.assertTrue(check_authority(self.profile, self.context, self.start).permitted)
        for context in [replace(self.context, occupancy_from=self.end), replace(self.context, occupancy_until=self.at)]:
            self.assertEqual(check_authority(self.profile, context, self.at).reason, "role-occupancy-outside-effective-period")

    def test_naive_datetimes_are_rejected(self) -> None:
        self.assertEqual(check_authority(self.profile, self.context, self.at.replace(tzinfo=None)).reason, "timezone-required")
        self.assertEqual(check_authority(replace(self.profile, valid_from=self.at.replace(tzinfo=None)), self.context, self.at).reason, "timezone-required")

    def test_exact_role_function_profession_facility_and_pathway(self) -> None:
        for context in [
            replace(self.context, role="different"), replace(self.context, function="different"),
            replace(self.context, profession="different"), replace(self.context, facility="different"),
            replace(self.context, pathway="different"),
        ]:
            result = check_authority(self.profile, context, self.at)
            self.assertEqual(result.reason, "missing-or-ambiguous-grant")
        self.assertFalse(check_authority(self.profile, replace(self.context, role="CMO"), self.at).permitted)

    def test_ambiguous_duplicate_and_empty_grants_are_rejected(self) -> None:
        for grants in [(), (self.grant, replace(self.grant, identifier="g2"))]:
            self.assertEqual(check_authority(replace(self.profile, grants=grants), self.context, self.at).reason, "missing-or-ambiguous-grant")
        for grants in [(self.grant, self.grant), (replace(self.grant, identifier=""),)]:
            self.assertEqual(check_authority(replace(self.profile, grants=grants), self.context, self.at).reason, "invalid-grant-identifiers")

    def test_committee_independence_is_function_specific(self) -> None:
        profile = replace(self.profile, grants=(replace(self.grant, independent_of_committee=True),))
        result = check_authority(profile, replace(self.context, participated_in_committee=True), self.at)
        self.assertEqual(result.reason, "committee-independence-required")
        self.assertTrue(check_authority(profile, self.context, self.at).permitted)
        self.assertFalse(check_authority(profile, replace(self.context, participated_in_committee=None), self.at).permitted)

    def test_conditions_cannot_be_skipped(self) -> None:
        result = check_authority(self.profile, replace(self.context, confirmed_conditions=frozenset()), self.at)
        self.assertEqual(result.reason, "required-condition-not-confirmed")

    def test_revocation_and_new_revision_do_not_rewrite_history(self) -> None:
        old = check_authority(self.profile, self.context, self.at)
        revoked = replace(self.profile, active=False, revision="2")
        self.assertFalse(check_authority(revoked, self.context, self.at).permitted)
        self.assertTrue(old.permitted)
        self.assertEqual(old.profile_revision, "1")

    def test_repeated_dst_hour_uses_absolute_instants(self) -> None:
        zone = ZoneInfo("America/New_York")
        expiry = datetime(2026, 11, 1, 1, 30, tzinfo=zone, fold=0)
        later = datetime(2026, 11, 1, 1, 15, tzinfo=zone, fold=1)
        start = datetime(2026, 10, 31, tzinfo=zone)
        end = datetime(2026, 11, 2, tzinfo=zone)
        profile = replace(self.profile, valid_from=start, valid_until=expiry)
        context = replace(self.context, occupancy_from=start, occupancy_until=end)
        self.assertEqual(check_authority(profile, context, later).reason, "profile-outside-effective-period")
        profile = replace(profile, valid_until=end)
        context = replace(context, occupancy_until=expiry)
        self.assertEqual(check_authority(profile, context, later).reason, "role-occupancy-outside-effective-period")
        earlier = datetime(2026, 11, 1, 1, 15, tzinfo=zone, fold=0)
        self.assertTrue(check_authority(profile, context, earlier).permitted)
