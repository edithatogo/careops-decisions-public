# Register storage codec

Status: executable reference; no tenant deployment or live register.
Continues [CareOps #160](https://github.com/edithatogo/careops/issues/160).

## Change

`register_codec.py` adds a small, versioned JSON boundary for the existing
`credentialing_register.Revision`. It is the first persistence slice, not a
new register model. There is no new practitioner form, approval or staff task.

The codec requires an exact schema, rejects duplicate fields and unsupported
versions, checks types without truth-value coercion, limits each payload to
65,536 UTF-8 bytes, and reuses existing domain validation. Timestamps must be
aware; writes use canonical UTC. Scope, limitations, supervision and explicit
supersession survive round trips. Invalid payload errors do not echo their
contents. Unknown fields are not silently discarded.

The codec does not resolve the history referred to by `supersedes`; the existing
register append operation must still do that. A later storage adapter must replay
and validate the entire history, enforce concurrency and commit atomically.

## Trust boundary

A parsed `human_confirmed: true` is a stored claim, not authentication or legal
authority. Neither JSON nor a content hash validates the actual decision maker.
Only an authenticated, authorised service with exact-decision-bound evidence may
write verified decisions. Never expose this decoder as a public approval API or
let a Copilot agent populate trusted decision receipts.

Use synthetic data only during qualification. Real register payloads, databases,
logs and snapshots do not belong in Git, public datasets or test fixtures.

## Qualification

The 13 synthetic test methods cover canonical round trips, timezone equivalence,
Unicode, retained supervision/conditions, supersession boundaries, all decision
states, malformed and oversized inputs, duplicate keys, unknown/missing fields,
invalid periods and literal-boolean/version checks. Local Python 3.13.5 coverage
of the new codec is 56 statements and 22 branches, all covered.

Existing repository CI must still pass on the final PR head, including strict
mypy, the configured correctness/security lint and 100% reference statement/branch
coverage on Python 3.13 and 3.14. No existing check or threshold is changed.
Passing these tests does not establish source authenticity or operational safety.

## Next slice

Add a synthetic/local SQLite persistence adapter with explicit initialization,
non-creating ordinary opens, atomic optimistic writes, exact replay protection,
restart tests, corruption detection and unchanged role-scoped projection. This
will exercise a durable repository boundary without choosing a CHHHS production
database. Authenticated hosting, records/security acceptance, backup/restore,
capacity tests, register UI and controlled migration remain separate gates.
