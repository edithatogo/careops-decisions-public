"""Synthetic boundary tests: JSON is never proof of decision authority."""
import json
import unittest
from dataclasses import replace
from datetime import UTC, datetime, timedelta, timezone
from typing import cast

from reference.credentialing_register import DecisionState, Register, Revision
from reference.register_codec import MAX_PAYLOAD_BYTES, decode_revision, encode_revision


def example() -> Revision:
    start = datetime(2026, 1, 1, tzinfo=UTC)
    return Revision(
        record_id="synthetic-record", decision_id="synthetic-decision",
        practitioner="synthetic-practitioner", profession="dental", facility="synthetic-site",
        scope="synthetic-scope", state=DecisionState.GRANTED,
        valid_from=start, valid_until=start + timedelta(days=30),
        effective_at=start, recorded_at=start,
        decision_reference="synthetic-decision-receipt", authority_reference="synthetic-authority",
        conditions=("Synthetic limitation",), supervision="Synthetic supervision",
        human_confirmed=True,
    )


class CodecTests(unittest.TestCase):
    def payload(self, **changes: object) -> str:
        data = cast(dict[str, object], json.loads(encode_revision(example())))
        data.update(changes)
        return json.dumps(data)

    def test_roundtrip_and_stable_encoding(self) -> None:
        value = example()
        self.assertEqual(decode_revision(encode_revision(value)), value)
        self.assertEqual(encode_revision(decode_revision(encode_revision(value))),
                         encode_revision(value))

    def test_offset_normalization_preserves_instant(self) -> None:
        value = example()
        offset = timezone(timedelta(hours=10))
        shifted = replace(value, recorded_at=value.recorded_at.astimezone(offset))
        self.assertEqual(encode_revision(shifted), encode_revision(value))
        self.assertEqual(decode_revision(self.payload(recorded_at="2026-01-01T10:00:00+10:00")),
                         value)

    def test_unicode_and_quotes_are_data(self) -> None:
        value = replace(example(), scope="Synthetic Māori / \"quoted\" scope",
                        conditions=("Synthetic <script> text",))
        self.assertEqual(decode_revision(encode_revision(value)), value)

    def test_supersession_requires_history_after_decoding(self) -> None:
        value = replace(example(), supersedes="synthetic-previous")
        decoded = decode_revision(encode_revision(value))
        self.assertEqual(decoded, value)
        with self.assertRaisesRegex(ValueError, "missing record"):
            Register().append(decoded, expected_version=0)

    def test_all_decision_states_and_empty_optional_fields(self) -> None:
        for state in DecisionState:
            value = replace(example(), state=state, conditions=(), supervision="")
            self.assertEqual(decode_revision(encode_revision(value)), value)

    def test_schema_is_exact(self) -> None:
        data = cast(dict[str, object], json.loads(encode_revision(example())))
        del data["supervision"]
        for payload in ("[]", "null", "1", "{}", json.dumps(data), self.payload(extra="value")):
            with self.subTest(payload=payload), self.assertRaises(ValueError):
                decode_revision(payload)

    def test_duplicate_keys_are_rejected(self) -> None:
        original = encode_revision(example())
        for key in ("human_confirmed", "schema_version"):
            with self.subTest(key=key), self.assertRaises(ValueError):
                decode_revision('{"' + key + '":null,' + original[1:])

    def test_schema_version_does_not_coerce(self) -> None:
        for version in (True, 1.0, "1", 2, None):
            with self.subTest(version=version), self.assertRaises(ValueError):
                decode_revision(self.payload(schema_version=version))

    def test_human_claim_must_be_literal_true(self) -> None:
        for claim in (False, 1, "true", None):
            with self.subTest(claim=claim), self.assertRaises(ValueError):
                decode_revision(self.payload(human_confirmed=claim))

    def test_text_and_condition_types_are_checked(self) -> None:
        for changes in ({"scope": 12}, {"state": False}, {"conditions": "limited"},
                        {"conditions": [1]}, {"conditions": [" "]}, {"record_id": ""}):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                decode_revision(self.payload(**changes))

    def test_time_and_domain_rules_are_checked(self) -> None:
        for changes in ({"valid_from": None}, {"recorded_at": "not-a-date"},
                        {"recorded_at": "2026-01-01T00:00:00"},
                        {"valid_until": "2026-01-01T00:00:00+00:00"},
                        {"state": "green"}):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                decode_revision(self.payload(**changes))

    def test_malformed_and_oversized_inputs_have_safe_errors(self) -> None:
        for payload in ("{private-content", " " * (MAX_PAYLOAD_BYTES + 1),
                        "[" * 2000, "\ud800"):
            with self.subTest(length=len(payload)), self.assertRaisesRegex(
                ValueError, "^Invalid register payload$"
            ):
                decode_revision(payload)

    def test_encoder_rejects_invalid_domain_values(self) -> None:
        for value in (replace(example(), human_confirmed=False),
                      replace(example(), recorded_at=datetime(2026, 1, 1)),
                      replace(example(), recorded_at=cast(datetime, "bad"))):
            with self.assertRaises(ValueError):
                encode_revision(value)
