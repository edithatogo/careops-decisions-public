"""Bind editable delegation content to separately verified release approval.

Hashes detect changes; they do not authenticate a person or confer legal power.
Receipt verification and protected storage belong to the authorised adapter.
"""
from dataclasses import dataclass, replace
from datetime import UTC, datetime
from hashlib import sha256
from json import dumps

from reference.delegations import Authority, Context, Profile, check_authority


def _utc(value: datetime) -> datetime:
    if value.tzinfo is None or value.utcoffset() is None:
        raise ValueError("Timezone-aware timestamp required")
    return value.astimezone(UTC)


def content_digest(profile: Profile) -> str:
    """Canonical digest of editable authority content, excluding release flags."""
    payload = {
        "profile": profile.identifier, "revision": profile.revision,
        "instrument": profile.instrument_reference, "editor": profile.edited_by,
        "from": _utc(profile.valid_from).isoformat(),
        "until": _utc(profile.valid_until).isoformat(),
        "grants": [
            {"id": grant.identifier, "function": grant.function, "role": grant.role,
             "professions": sorted(grant.professions), "facilities": sorted(grant.facilities),
             "pathways": sorted(grant.pathways), "conditions": sorted(grant.required_conditions),
             "independent": grant.independent_of_committee}
            for grant in sorted(profile.grants, key=lambda item: item.identifier)
        ],
    }
    return sha256(dumps(payload, sort_keys=True, separators=(",", ":")).encode()).hexdigest()


@dataclass(frozen=True)
class Receipt:
    """Authenticated approval evidence supplied by a protected service, not JSON."""

    content_hash: str
    instrument_reference: str
    instrument_hash: str
    approver: str
    approval_reference: str
    approved_at: datetime
    current_until: datetime
    verified: bool = False


@dataclass(frozen=True)
class Release:
    profile: Profile
    receipt: Receipt


def _validate(profile: Profile, receipt: Receipt, at: datetime) -> None:
    instant = _utc(at)
    approved = _utc(receipt.approved_at)
    expires = _utc(receipt.current_until)
    fields = (profile.identifier, profile.revision, profile.instrument_reference,
              profile.edited_by, receipt.approver, receipt.approval_reference)
    if not all(value.strip() for value in fields) or receipt.verified is not True:
        raise ValueError("Verified independent release evidence required")
    if receipt.approver == profile.edited_by:
        raise ValueError("Editors cannot approve their own authority revision")
    if not approved <= instant < expires:
        raise ValueError("Release verification is not current")
    if receipt.instrument_reference != profile.instrument_reference:
        raise ValueError("Instrument reference mismatch")
    if len(receipt.instrument_hash) != 64 or any(c not in "0123456789abcdef" for c in receipt.instrument_hash):
        raise ValueError("Verified source hash required")
    if content_digest(profile) != receipt.content_hash:
        raise ValueError("Authority content changed after approval")
    if _utc(profile.valid_until) <= _utc(profile.valid_from):
        raise ValueError("Invalid authority period")
    identifiers = [grant.identifier for grant in profile.grants]
    if not identifiers or not all(identifier.strip() for identifier in identifiers) or len(set(identifiers)) != len(identifiers):
        raise ValueError("Nonempty unique grant identifiers required")


def publish_profile(draft: Profile, receipt: Receipt, at: datetime) -> Release:
    """Produce an immutable released snapshot; no tenant state is changed."""
    if draft.active is not False or draft.controlled_source_verified is not False or draft.approved_by or draft.approval_reference:
        raise ValueError("Only an unapproved draft can enter release")
    _validate(draft, receipt, at)
    profile = replace(draft, active=True, controlled_source_verified=True,
                      approved_by=receipt.approver, approval_reference=receipt.approval_reference)
    return Release(profile, receipt)


def check_released_authority(release: Release, context: Context, at: datetime) -> Authority:
    """Revalidate the exact release and decision-time occupancy on each action."""
    try:
        _validate(release.profile, release.receipt, at)
        if (release.profile.approved_by, release.profile.approval_reference) != (
            release.receipt.approver, release.receipt.approval_reference,
        ):
            raise ValueError("Release approval metadata mismatch")
    except ValueError:
        return Authority(False, "invalid-or-stale-authority-release", release.profile.revision)
    return check_authority(release.profile, context, at)
