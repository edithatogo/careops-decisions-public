# Native delegation gate: N02.06/N02.07 source slice

24 September 2026. Follows Decisions#14 and portfolio CareOps#165.
Programme: https://github.com/edithatogo/careops/issues/160

This net462 library brings the existing reference delegation/release invariants
into the native Microsoft delivery path. It adds no Python, SQLite, cloud,
app-registration, sibling-module or AI runtime dependency. The no-admin M365
pilot remains independently available and does not install this component.

## Behaviour

The typed immutable profile binds organization, profile ID, revision, editor,
controlled instrument reference/hash, effective period and every grant field.
The native v1 digest has a version prefix, length-delimited strings/counts,
ordinal sorting and UTC tick instants. It is intentionally NOT the Python JSON
reference digest; native receipts must record the digest version. Do not reuse
an old reference receipt, silently translate a hash, or treat a hash as a signature.

Changing a role, function, profession/facility/pathway set, condition, independence
requirement, source, period or version invalidates prior approval. Reordering a
set or representing the same instant with another offset does not. Collection
inputs are copied; identifiers are exact and not silently trimmed or aliased.
Engineering bounds (128 grants/set items, 256 identifier characters) are not
clinical or legal policy limits. Adapters must use bounded, materialized collections.

Each action rechecks source verification, withdrawal, independent profile approval,
release currentness, profile effective period, role occupancy, conflicts, one
unambiguous applicable grant, committee independence and all conditions. Denial
stops this attempted action only; it does not revoke a prior scope or manufacture
an adverse practitioner finding. Malformed configuration is a technical error
for its owner, not an applicant evidence deficiency.

## Critical trusted boundary

The objects are NOT request/JSON DTOs and this library does not authenticate any
person. A protected native host must derive organization and human principal from
its runtime, read current released profile/approval/occupancy/conflict evidence
under authorized access, and obtain server time. A caller cannot supply those
trusted facts, a sourceVerified flag or an approval record. Protect direct CRUD,
role occupancy and source-verification fields; recheck within the authoritative
write transaction. A successful Check is not a signed decision receipt and must
not be cached across transactions, case revisions or role/authority changes.

This slice does not yet build that Dataverse adapter, approve CHHHS delegations,
assign authority to CMO from an EDMS alias, or deploy/register a plug-in. Current
signed source reconciliation, native entity/security configuration, independent
profile approval UI and native sandbox tests remain N02 gates.

## Qualification

Tests target net48 on the actual Windows runner while compiling the library for
net462. Existing reference/calendar/Validate checks stay unchanged. New CI requires
warnings-as-errors, all tests executed, nonempty whole-library 100% line/branch
coverage, and qualification artifacts. Commit and review generated dependency
locks and repeat locked restore before merging. No local .NET execution is claimed.

The tests use synthetic identities/grants, every editable-field mutation, exact
scope matching, ambiguous grants, self-approval/self-decision, source withdrawal,
time boundaries, unknown conflicts/participation, malformed hashes/identifiers,
invalid Unicode, copied collections and equivalent set/offset representations.
Source acceptance is not legal, clinical, independent security or tenant approval.

Primary implementation reference:
https://learn.microsoft.com/en-us/power-apps/developer/data-platform/build-and-package
