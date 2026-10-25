"""Fictional meeting-packet identity and optional-context contracts.

This pure reference validates linked record identity. It does not approve, persist,
copy, generate, publish, or synchronize governance records.
"""
from dataclasses import dataclass
from datetime import datetime, timedelta
from enum import StrEnum


class ProviderStatus(StrEnum):
    ABSENT = "provider-absent"
    READY = "snapshot-ready"
    MISSING = "snapshot-input-missing"
    INCOMPATIBLE = "provider-incompatible"
    STALE = "snapshot-stale-or-future"


@dataclass(frozen=True)
class AgendaItem:
    item_id: str
    meeting_id: str
    revision: int


@dataclass(frozen=True)
class MinutesRevision:
    minutes_id: str
    meeting_id: str
    revision: int
    content_hash: str
    human_edited: bool


@dataclass(frozen=True)
class DecisionRecord:
    decision_id: str
    agenda_item_id: str
    minutes_id: str
    minutes_revision: int
    revision: int = 1


@dataclass(frozen=True)
class ActionRecord:
    action_id: str
    agenda_item_id: str
    decision_id: str = ""
    revision: int = 1


@dataclass(frozen=True)
class GovernancePacket:
    meeting_id: str
    agenda_items: tuple[AgendaItem, ...]
    minutes: tuple[MinutesRevision, ...]
    decisions: tuple[DecisionRecord, ...]
    actions: tuple[ActionRecord, ...]


@dataclass(frozen=True)
class OptionalSnapshot:
    provider_id: str
    contract_version: str
    snapshot_id: str
    meeting_id: str
    captured_at: datetime | None
    correlation_id: str
    idempotency_key: str


@dataclass(frozen=True)
class IdempotencyRecord:
    key: str
    payload_hash: str


def _require_text(value: str, field: str) -> None:
    if not isinstance(value, str) or not value.strip():
        raise ValueError(f"{field} is required")


def validate_packet(packet: GovernancePacket) -> None:
    """Validate one fictional packet's links without changing its records."""
    _require_text(packet.meeting_id, "meeting_id")
    agenda_by_id: dict[str, AgendaItem] = {}
    for agenda_item in packet.agenda_items:
        _require_text(agenda_item.item_id, "agenda item id")
        if agenda_item.meeting_id != packet.meeting_id or agenda_item.revision < 1:
            raise ValueError("agenda item meeting or revision is invalid")
        if agenda_item.item_id in agenda_by_id:
            raise ValueError("duplicate agenda item id")
        agenda_by_id[agenda_item.item_id] = agenda_item

    minutes_by_revision: dict[tuple[str, int], MinutesRevision] = {}
    for minutes_revision in packet.minutes:
        _require_text(minutes_revision.minutes_id, "minutes id")
        _require_text(minutes_revision.content_hash, "minutes content hash")
        if minutes_revision.meeting_id != packet.meeting_id or minutes_revision.revision < 1:
            raise ValueError("minutes meeting or revision is invalid")
        key = (minutes_revision.minutes_id, minutes_revision.revision)
        if key in minutes_by_revision:
            raise ValueError("duplicate minutes revision")
        minutes_by_revision[key] = minutes_revision

    decisions_by_id: dict[str, DecisionRecord] = {}
    for decision_record in packet.decisions:
        _require_text(decision_record.decision_id, "decision id")
        if decision_record.revision < 1:
            raise ValueError("decision revision must be positive")
        if decision_record.decision_id in decisions_by_id:
            raise ValueError("duplicate decision id")
        if decision_record.agenda_item_id not in agenda_by_id:
            raise ValueError("decision must reference an agenda item in this meeting")
        if (decision_record.minutes_id, decision_record.minutes_revision) not in minutes_by_revision:
            raise ValueError("decision must reference an existing minutes revision")
        decisions_by_id[decision_record.decision_id] = decision_record

    action_ids: set[str] = set()
    for action_record in packet.actions:
        _require_text(action_record.action_id, "action id")
        if action_record.revision < 1:
            raise ValueError("action revision must be positive")
        if action_record.action_id in action_ids:
            raise ValueError("duplicate action id")
        action_ids.add(action_record.action_id)
        if action_record.agenda_item_id not in agenda_by_id:
            raise ValueError("action must reference an agenda item in this meeting")
        if action_record.decision_id:
            decision = decisions_by_id.get(action_record.decision_id)
            if decision is None:
                raise ValueError("action must reference an existing decision")
            if decision.agenda_item_id != action_record.agenda_item_id:
                raise ValueError("action and decision agenda identity must match")


def select_minutes_revision(
    current: MinutesRevision | None, candidate: MinutesRevision
) -> tuple[MinutesRevision, str]:
    """Select a preview revision while protecting human-edited current minutes."""
    if current is None:
        return candidate, "candidate-selected"
    if current.minutes_id != candidate.minutes_id or current.meeting_id != candidate.meeting_id:
        raise ValueError("minutes identity cannot change during revision selection")
    if candidate.revision <= current.revision:
        raise ValueError("candidate revision must be newer")
    if current.human_edited:
        return current, "human-edit-preserved"
    return candidate, "candidate-selected"


def evaluate_optional_snapshot(
    snapshot: OptionalSnapshot | None,
    *,
    expected_meeting_id: str,
    expected_contract_version: str,
    now: datetime,
    max_age: timedelta,
) -> ProviderStatus:
    """Classify optional read-only context metadata; never imports its payload."""
    _require_text(expected_meeting_id, "expected meeting id")
    _require_text(expected_contract_version, "expected contract version")
    if now.tzinfo is None or now.utcoffset() is None:
        raise ValueError("now must be timezone-aware")
    if max_age <= timedelta(0):
        raise ValueError("max_age must be positive")
    if snapshot is None:
        return ProviderStatus.ABSENT
    identity = (
        snapshot.provider_id,
        snapshot.contract_version,
        snapshot.snapshot_id,
        snapshot.meeting_id,
        snapshot.correlation_id,
        snapshot.idempotency_key,
    )
    if any(not isinstance(value, str) or not value.strip() for value in identity) or snapshot.captured_at is None:
        return ProviderStatus.MISSING
    if (snapshot.meeting_id != expected_meeting_id
            or snapshot.contract_version != expected_contract_version):
        return ProviderStatus.INCOMPATIBLE
    captured_at = snapshot.captured_at
    if captured_at.tzinfo is None or captured_at.utcoffset() is None:
        return ProviderStatus.STALE
    age = now - captured_at
    if age < timedelta(0) or age > max_age:
        return ProviderStatus.STALE
    return ProviderStatus.READY


def classify_retry(
    history: tuple[IdempotencyRecord, ...], *, key: str, payload_hash: str
) -> str:
    """Report retry disposition without performing a write or external effect."""
    _require_text(key, "idempotency key")
    _require_text(payload_hash, "payload hash")
    matches = tuple(item for item in history if item.key == key)
    if len(matches) > 1:
        raise ValueError("idempotency history contains duplicate keys")
    if not matches:
        return "new"
    if matches[0].payload_hash == payload_hash:
        return "duplicate"
    raise ValueError("idempotency key reused with different content")
