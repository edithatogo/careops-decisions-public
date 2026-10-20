"""Strict, versioned JSON for storage of verified register decisions.

Parsing validates structure, not identity, legal authority or source authenticity.
Never accept a decoded human_confirmed claim as an authenticated approval.
"""
import json
from dataclasses import asdict, replace
from datetime import UTC, datetime
from typing import cast

from reference.credentialing_register import DecisionState, Register, Revision

MAX_PAYLOAD_BYTES = 65_536
TEXT_FIELDS = (
    "record_id", "decision_id", "practitioner", "profession", "facility", "scope",
    "decision_reference", "authority_reference", "supervision", "supersedes",
)
TIME_FIELDS = ("valid_from", "valid_until", "effective_at", "recorded_at")
EXPECTED_FIELDS = frozenset((*TEXT_FIELDS, *TIME_FIELDS, "state", "conditions",
                             "human_confirmed", "schema_version"))


def _object(pairs: list[tuple[str, object]]) -> dict[str, object]:
    result: dict[str, object] = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate register field")
        result[key] = value
    return result


def _text(data: dict[str, object], key: str) -> str:
    value = data[key]
    if not isinstance(value, str):
        raise ValueError("Text field required")
    return value


def decode_revision(payload: str) -> Revision:
    """Reject ambiguous, unknown, oversized or malformed persisted records.

    No default values, truth-value coercions or timezone assumptions are used.
    Full history/supersession validation remains the register's responsibility.
    """
    try:
        if len(payload.encode("utf-8")) > MAX_PAYLOAD_BYTES:
            raise ValueError("Payload limit exceeded")
        parsed: object = json.loads(payload, object_pairs_hook=_object)
        if not isinstance(parsed, dict) or set(parsed) != EXPECTED_FIELDS:
            raise ValueError("Exact register schema required")
        data = cast(dict[str, object], parsed)
        if type(data["schema_version"]) is not int or data["schema_version"] != 1:
            raise ValueError("Unsupported register schema")
        if data["human_confirmed"] is not True:
            raise ValueError("Human decision claim required")
        conditions = data["conditions"]
        if not isinstance(conditions, list):
            raise ValueError("Conditions must be a list")
        if any(not isinstance(item, str) or not item.strip() for item in conditions):
            raise ValueError("Nonempty condition text required")
        texts = {key: _text(data, key) for key in TEXT_FIELDS}
        times = {key: datetime.fromisoformat(_text(data, key)) for key in TIME_FIELDS}
        revision = Revision(
            record_id=texts["record_id"], decision_id=texts["decision_id"],
            practitioner=texts["practitioner"], profession=texts["profession"],
            facility=texts["facility"], scope=texts["scope"],
            state=DecisionState(_text(data, "state")),
            valid_from=times["valid_from"], valid_until=times["valid_until"],
            effective_at=times["effective_at"], recorded_at=times["recorded_at"],
            decision_reference=texts["decision_reference"],
            authority_reference=texts["authority_reference"],
            conditions=tuple(cast(list[str], conditions)), supervision=texts["supervision"],
            supersedes=texts["supersedes"], human_confirmed=True,
        )
        # Reuse existing domain validation without pretending to validate history.
        normalized = Register().append(replace(revision, supersedes=""), expected_version=0)
        return replace(normalized.history[0], supersedes=revision.supersedes)
    except (ValueError, TypeError, RecursionError):
        raise ValueError("Invalid register payload") from None


def encode_revision(revision: Revision) -> str:
    """Write canonical UTC JSON; validate the wire representation before storage."""
    data: dict[str, object] = asdict(revision)
    for key in TIME_FIELDS:
        value: object = data[key]
        if not isinstance(value, datetime) or value.utcoffset() is None:
            raise ValueError("Timezone-aware timestamps required")
        data[key] = value.astimezone(UTC).isoformat()
    data["schema_version"] = 1
    payload = json.dumps(data, sort_keys=True, separators=(",", ":"), ensure_ascii=True)
    decode_revision(payload)
    return payload
