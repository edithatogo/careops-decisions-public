"""Immutable register reference with explicit revision and access boundaries.

This is an in-memory domain model, not a database or authentication service.
Only a protected adapter may supply verified decision receipts and viewer grants.
"""
from dataclasses import dataclass, replace
from datetime import UTC, datetime
from enum import StrEnum


class DecisionState(StrEnum):
    GRANTED = "granted"
    SUSPENDED = "suspended"
    REVOKED = "revoked"
    REFUSED = "refused"


class ViewRole(StrEnum):
    OPERATIONAL = "operational"
    CREDENTIALING = "credentialing"
    AUDITOR = "auditor"
    PRACTITIONER = "practitioner"


@dataclass(frozen=True)
class Revision:
    record_id: str
    decision_id: str
    practitioner: str
    profession: str
    facility: str
    scope: str
    state: DecisionState
    valid_from: datetime
    valid_until: datetime
    effective_at: datetime
    recorded_at: datetime
    decision_reference: str
    authority_reference: str
    conditions: tuple[str, ...] = ()
    supervision: str = ""
    supersedes: str = ""
    human_confirmed: bool = False


@dataclass(frozen=True)
class Register:
    history: tuple[Revision, ...] = ()

    def append(self, revision: Revision, *, expected_version: int) -> Register:
        """Append a verified human decision; exact replays are harmless.

        Durable optimistic locking and authenticated receipts are adapter gates.
        UTC normalisation makes replay equality safe through repeated DST hours.
        """
        times = (revision.valid_from, revision.valid_until, revision.effective_at, revision.recorded_at)
        if any(value.tzinfo is None or value.utcoffset() is None for value in times):
            raise ValueError("Timezone-aware timestamps required")
        revision = replace(revision, valid_from=revision.valid_from.astimezone(UTC),
                           valid_until=revision.valid_until.astimezone(UTC),
                           effective_at=revision.effective_at.astimezone(UTC),
                           recorded_at=revision.recorded_at.astimezone(UTC))
        fields = (revision.record_id, revision.decision_id, revision.practitioner,
                  revision.profession, revision.facility, revision.scope,
                  revision.decision_reference, revision.authority_reference)
        if not all(field.strip() for field in fields) or revision.human_confirmed is not True:
            raise ValueError("Verified decision and authority evidence required")
        if not isinstance(revision.state, DecisionState) or revision.valid_until <= revision.valid_from:
            raise ValueError("Valid decision state and period required")
        existing = tuple(item for item in self.history if item.decision_id == revision.decision_id)
        if existing:
            if existing[0] != revision:
                raise ValueError("Decision identifier reused for different content")
            return self
        if expected_version != len(self.history):
            raise ValueError("Concurrent register change; reconcile before retry")
        previous = tuple(item for item in self.history if item.record_id == revision.record_id)
        if previous:
            old = previous[-1]
            if revision.supersedes != old.decision_id:
                raise ValueError("Explicit latest supersession required")
            if (revision.practitioner, revision.profession, revision.facility) != (old.practitioner, old.profession, old.facility):
                raise ValueError("A record cannot be moved to another practitioner or service")
            if revision.recorded_at < old.recorded_at:
                raise ValueError("Recorded chronology cannot move backwards")
        elif revision.supersedes:
            raise ValueError("Cannot supersede a missing record")
        return Register((*self.history, revision))


@dataclass(frozen=True)
class Viewer:
    principal: str
    role: ViewRole
    facilities: frozenset[str]
    professions: frozenset[str]
    practitioner: str = ""


@dataclass(frozen=True)
class RegisterRow:
    practitioner: str
    profession: str
    facility: str
    scope: str
    recorded_state: DecisionState
    status: str
    valid_from: datetime
    valid_until: datetime
    conditions: tuple[str, ...]
    supervision: str
    decision_reference: str
    authority_reference: str


@dataclass(frozen=True)
class RegisterView:
    as_of: datetime
    currentness_verified: bool
    rows: tuple[RegisterRow, ...]


def project(register: Register, viewer: Viewer, at: datetime, *, current_until: datetime) -> RegisterView:
    """Produce a minimum-necessary view from an authenticated viewer context.

    Lease expiry reports currentness unknown, not permission or scope revocation.
    Case documents, reasons and application RAG scores are never projected.
    """
    if not viewer.principal.strip() or not isinstance(viewer.role, ViewRole):
        raise PermissionError("Authenticated register role required")
    if any(value.tzinfo is None or value.utcoffset() is None for value in (at, current_until)):
        raise ValueError("Timezone-aware timestamps required")
    instant = at.astimezone(UTC)
    selected: dict[str, Revision] = {}
    for revision in register.history:
        if revision.recorded_at <= instant and revision.effective_at <= instant:
            selected[revision.record_id] = revision
    rows: list[RegisterRow] = []
    for revision in selected.values():
        if revision.facility not in viewer.facilities or revision.profession not in viewer.professions:
            continue
        if viewer.role == ViewRole.PRACTITIONER and revision.practitioner != viewer.practitioner:
            continue
        if instant >= current_until.astimezone(UTC):
            status = "currentness-unverified"
        elif revision.state != DecisionState.GRANTED:
            status = revision.state.value
        elif instant < revision.valid_from:
            status = "not-yet-effective"
        elif instant >= revision.valid_until:
            status = "expired"
        else:
            status = "scope-recorded"
        detailed = viewer.role != ViewRole.OPERATIONAL
        rows.append(RegisterRow(
            revision.practitioner, revision.profession, revision.facility,
            revision.scope, revision.state, status, revision.valid_from, revision.valid_until,
            revision.conditions, revision.supervision,
            revision.decision_reference if detailed else "",
            revision.authority_reference if detailed else "",
        ))
    return RegisterView(instant, instant < current_until.astimezone(UTC), tuple(rows))
