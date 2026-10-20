"""Pure authority predicates for trusted, versioned delegation configurations.

This checks a recorded authority mapping, not clinical suitability. A protected
adapter must verify instruments, approvals, role occupancy and conditions before
constructing these objects. No UI or agent may self-assert those verifications.
"""
from dataclasses import dataclass
from datetime import UTC, datetime


@dataclass(frozen=True)
class Grant:
    """An editable function-specific grant in an approved profile revision."""

    identifier: str
    function: str
    role: str
    professions: frozenset[str]
    facilities: frozenset[str]
    pathways: frozenset[str]
    required_conditions: frozenset[str] = frozenset()
    independent_of_committee: bool = False


@dataclass(frozen=True)
class Profile:
    """Authority release signed off by someone other than its editor."""

    identifier: str
    revision: str
    instrument_reference: str
    approval_reference: str
    edited_by: str
    approved_by: str
    valid_from: datetime
    valid_until: datetime
    grants: tuple[Grant, ...]
    active: bool = False
    controlled_source_verified: bool = False


@dataclass(frozen=True)
class Context:
    """Case and occupancy evidence obtained from authenticated services."""

    actor: str
    practitioner: str
    role: str
    occupancy_reference: str
    occupancy_from: datetime
    occupancy_until: datetime
    function: str
    profession: str
    facility: str
    pathway: str
    confirmed_conditions: frozenset[str] = frozenset()
    conflict: bool | None = None
    participated_in_committee: bool | None = None


@dataclass(frozen=True)
class Authority:
    """Permission to exercise one function; not a credentialing decision."""

    permitted: bool
    reason: str
    profile_revision: str
    grant_reference: str = ""


def check_authority(profile: Profile, context: Context, at: datetime) -> Authority:
    """Evaluate authority at decision time, with half-open validity intervals.

    Unknown/expired/ambiguous mappings fail closed for this action. They do not
    revoke prior decisions or disable an independently authorised human pathway.
    Role titles are exact identifiers; aliases need an approved profile change.
    """
    def denied(reason: str) -> Authority:
        return Authority(False, reason, profile.revision)

    dates = (at, profile.valid_from, profile.valid_until, context.occupancy_from, context.occupancy_until)
    if any(value.tzinfo is None or value.utcoffset() is None for value in dates):
        return denied("timezone-required")
    if profile.active is not True or profile.controlled_source_verified is not True:
        return denied("unverified-or-inactive-profile")
    identity_fields = (
        profile.identifier, profile.revision, profile.instrument_reference,
        profile.approval_reference, profile.edited_by, profile.approved_by,
        context.actor, context.practitioner, context.role, context.occupancy_reference,
        context.function, context.profession, context.facility, context.pathway,
    )
    if not all(value.strip() for value in identity_fields):
        return denied("missing-authority-evidence")
    if profile.edited_by == profile.approved_by:
        return denied("configuration-self-approval")
    instant = at.astimezone(UTC)
    if not profile.valid_from.astimezone(UTC) <= instant < profile.valid_until.astimezone(UTC):
        return denied("profile-outside-effective-period")
    if not context.occupancy_from.astimezone(UTC) <= instant < context.occupancy_until.astimezone(UTC):
        return denied("role-occupancy-outside-effective-period")
    if context.actor == context.practitioner or context.conflict is not False:
        return denied("self-decision-or-conflict")
    identifiers = [grant.identifier for grant in profile.grants]
    if not all(identifier.strip() for identifier in identifiers) or len(set(identifiers)) != len(identifiers):
        return denied("invalid-grant-identifiers")
    matches = tuple(
        grant for grant in profile.grants
        if grant.function == context.function and grant.role == context.role
        and context.profession in grant.professions
        and context.facility in grant.facilities and context.pathway in grant.pathways
    )
    if len(matches) != 1:
        return denied("missing-or-ambiguous-grant")
    grant = matches[0]
    if grant.independent_of_committee and context.participated_in_committee is not False:
        return denied("committee-independence-required")
    if not grant.required_conditions <= context.confirmed_conditions:
        return denied("required-condition-not-confirmed")
    return Authority(True, "verified-function-authority", profile.revision, grant.identifier)
