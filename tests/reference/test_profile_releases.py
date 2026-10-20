"""Synthetic approval binding; no CHHHS live identities or instruments."""
import unittest
from dataclasses import replace
from datetime import UTC, datetime, timedelta, timezone

from reference.delegations import Context, Grant, Profile
from reference.profile_releases import (
    Receipt,
    check_released_authority,
    content_digest,
    publish_profile,
)


class ReleaseTest(unittest.TestCase):
    def setUp(self) -> None:
        self.at = datetime(2026, 9, 22, tzinfo=UTC)
        self.before = self.at - timedelta(days=1)
        self.after = self.at + timedelta(days=1)
        self.grant = Grant("g1", "ordinary", "delegate", frozenset({"medicine"}),
                           frozenset({"site-1"}), frozenset({"renewal"}))
        self.draft = Profile("profile-1", "1", "instrument-1", "", "editor-1", "",
                             self.before, self.after, (self.grant,))
        self.receipt = self.receipt_for(self.draft)
        self.context = Context("actor-1", "practitioner-1", "delegate", "occupancy-1",
                               self.before, self.after, "ordinary", "medicine", "site-1",
                               "renewal", conflict=False, participated_in_committee=False)

    def receipt_for(self, profile: Profile) -> Receipt:
        return Receipt(content_digest(profile), profile.instrument_reference, "a" * 64,
                       "approver-1", "approval-1", self.before, self.after, verified=True)

    def test_publish_binds_content_without_modifying_the_draft(self) -> None:
        release = publish_profile(self.draft, self.receipt, self.at)
        self.assertTrue(release.profile.active)
        self.assertFalse(self.draft.active)
        self.assertEqual(release.profile.approval_reference, "approval-1")
        result = check_released_authority(release, self.context, self.at)
        self.assertTrue(result.permitted)
        self.assertEqual(result.grant_reference, "g1")

    def test_authority_edit_requires_a_new_approval(self) -> None:
        edited = replace(self.draft, grants=(replace(self.grant, role="other"),))
        with self.assertRaises(ValueError):
            publish_profile(edited, self.receipt, self.at)
        release = publish_profile(self.draft, self.receipt, self.at)
        tampered = replace(release, profile=replace(release.profile, grants=edited.grants))
        self.assertFalse(check_released_authority(tampered, self.context, self.at).permitted)

    def test_self_approval_and_unverified_receipts_are_rejected(self) -> None:
        for receipt in (replace(self.receipt, approver="editor-1"),
                        replace(self.receipt, verified=False),
                        replace(self.receipt, approval_reference="")):
            with self.subTest(receipt=receipt), self.assertRaises(ValueError):
                publish_profile(self.draft, receipt, self.at)

    def test_stale_future_and_mismatched_sources_are_rejected(self) -> None:
        for receipt in (replace(self.receipt, current_until=self.at),
                        replace(self.receipt, approved_at=self.after),
                        replace(self.receipt, instrument_reference="different"),
                        replace(self.receipt, instrument_hash="short"),
                        replace(self.receipt, instrument_hash="z" * 64)):
            with self.subTest(receipt=receipt), self.assertRaises(ValueError):
                publish_profile(self.draft, receipt, self.at)

    def test_naive_timestamps_are_rejected(self) -> None:
        with self.assertRaises(ValueError):
            publish_profile(self.draft, self.receipt, self.at.replace(tzinfo=None))
        with self.assertRaises(ValueError):
            content_digest(replace(self.draft, valid_from=self.before.replace(tzinfo=None)))

    def test_invalid_period_empty_and_duplicate_grants_are_rejected(self) -> None:
        for draft in (replace(self.draft, valid_until=self.before),
                      replace(self.draft, grants=()),
                      replace(self.draft, grants=(self.grant, self.grant)),
                      replace(self.draft, grants=(replace(self.grant, identifier=""),))):
            with self.subTest(draft=draft), self.assertRaises(ValueError):
                publish_profile(draft, self.receipt_for(draft), self.at)

    def test_client_flags_cannot_promote_a_draft(self) -> None:
        for draft in (replace(self.draft, active=True),
                      replace(self.draft, controlled_source_verified=True),
                      replace(self.draft, approved_by="approver-1"),
                      replace(self.draft, approval_reference="approval-1")):
            with self.subTest(draft=draft), self.assertRaises(ValueError):
                publish_profile(draft, self.receipt, self.at)

    def test_post_release_approval_metadata_tampering_is_rejected(self) -> None:
        release = publish_profile(self.draft, self.receipt, self.at)
        changed = replace(release, profile=replace(release.profile, approved_by="different"))
        result = check_released_authority(changed, self.context, self.at)
        self.assertFalse(result.permitted)
        self.assertEqual(result.reason, "invalid-or-stale-authority-release")

    def test_decision_time_rechecks_occupancy_conflict_and_function(self) -> None:
        release = publish_profile(self.draft, self.receipt, self.at)
        for context in (replace(self.context, occupancy_until=self.at),
                        replace(self.context, conflict=True),
                        replace(self.context, function="appeal")):
            with self.subTest(context=context):
                self.assertFalse(check_released_authority(release, context, self.at).permitted)
        stale = check_released_authority(release, self.context, self.after)
        self.assertFalse(stale.permitted)
        self.assertTrue(release.profile.active)

    def test_canonical_digest_is_order_and_timezone_invariant(self) -> None:
        second = replace(self.grant, identifier="g2", role="other")
        profile = replace(self.draft, grants=(self.grant, second))
        equivalent = replace(profile, grants=(second, self.grant),
                             valid_from=profile.valid_from.astimezone(timezone(timedelta(hours=10))))
        self.assertEqual(content_digest(profile), content_digest(equivalent))

    def test_each_editable_scope_and_condition_is_bound(self) -> None:
        for grant in (replace(self.grant, function="appeal"),
                      replace(self.grant, professions=frozenset({"nursing"})),
                      replace(self.grant, facilities=frozenset({"site-2"})),
                      replace(self.grant, pathways=frozenset({"temporary"})),
                      replace(self.grant, required_conditions=frozenset({"peer-review"})),
                      replace(self.grant, independent_of_committee=True)):
            with self.subTest(grant=grant):
                self.assertNotEqual(content_digest(self.draft), content_digest(replace(self.draft, grants=(grant,))))

    def test_revised_approved_profile_preserves_the_previous_release(self) -> None:
        original = publish_profile(self.draft, self.receipt, self.at)
        revised = replace(self.draft, revision="2", grants=(replace(self.grant, role="other"),))
        newer = publish_profile(revised, self.receipt_for(revised), self.at)
        self.assertEqual(original.profile.revision, "1")
        self.assertEqual(newer.profile.revision, "2")
        self.assertFalse(check_released_authority(newer, self.context, self.at).permitted)

    def test_profile_metadata_and_validity_edits_cannot_reuse_an_approval(self) -> None:
        for draft in (replace(self.draft, identifier="another-profile"),
                      replace(self.draft, revision="2"),
                      replace(self.draft, instrument_reference="instrument-2"),
                      replace(self.draft, edited_by="another-editor"),
                      replace(self.draft, valid_from=self.before - timedelta(hours=1)),
                      replace(self.draft, valid_until=self.after + timedelta(hours=1))):
            with self.subTest(draft=draft), self.assertRaises(ValueError):
                publish_profile(draft, self.receipt, self.at)
