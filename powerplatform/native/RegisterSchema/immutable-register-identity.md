# Preserve the identity of an existing credentialing record

25 September 2026. N03.02/N03.04 source slice after Decisions#31 at
54c0ee87700a6f8b1ae73e21b38fb63a592d596f. Not deployed to a tenant.

ProtectRegisterIdentity is an actual IPlugin implementation. It compares four
identity fields in native pre/post images for every synchronous Update of the
standard dft_credentialingrecord table: canonical record key, practitioner source
reference, profession reference and facility reference. It cannot reassign a
record and all its attached decision history by changing one of those labels.
An unchanged identity permits the original update to continue; it does not grant
permission to make that update. Scope/status/condition/version operations remain
subject to the separate authority-bound write handler and ordinary platform roles.

The guard performs no service calls, writes, retry, notifications or automated
repair. It does not add a staff approval or data-entry step. A genuine correction
of identity must use a separately designed, authorised reconciliation process
that preserves the original history. It must not be implemented by bypassing this
check or inventing an alias based on display names. That correction path is open.

## Required native registration

Register once on Update, dft_credentialingrecord, synchronous PostOperation (40),
server only, calling-user context. Supply CareOpsIdentityBefore (pre-image) and
CareOpsIdentityAfter (post-image), each containing only the four ImageColumns.
No filtering attributes: this is a deliberate integrity check of the resulting
row, including changes introduced by earlier pre-operation extensions even when
an identity field was absent from the incoming Target. Four small strings are
compared in memory; the actual cost still needs representative native measurement.
Do not select all columns. Missing/mismatched images stop the update.

PostOperation is inside the database transaction. Throwing must escape to the
platform for rollback; the code does not perform compensating writes. This choice
trades a late rollback for inspection of the actual post-image, rather than
trusting a mutable caller patch or a prior plugin's interpretation. No depth-based
skip, actor-specific exemption, admin label or caller-supplied old image is accepted.
No plugin registration, permission or platform bypass setting is changed here.

Microsoft documents that existing single-operation plugins still apply to
UpdateMultiple through the merged pipeline. This handler is NOT separately
registered on UpdateMultiple. Verify standard-table bulk/Upsert/specialized update
paths, nested updates, failed transaction rollback and actual image behavior in
the approved sandbox. There is no claim of measured native coverage from a mock.

Creation integrity, deletion/retention, decision/event immutability, authoritative
write authorization and administrative bypass controls are distinct unfinished
requirements. Restrict and audit permissions that can disable/bypass custom logic.
This is not a complete direct-CRUD firewall or legal-source approval. A missing
configuration image is a technical problem, not an adverse practitioner finding.

## Source qualification

Tests exercise the actual plugin class with synthetic execution contexts, exact
identity changes, unchanged/nested updates, all missing images/fields, malformed
references, pipeline mismatches and generic fault handling. Existing SDK projects,
locked packages, warnings-as-errors and whole-library line/branch coverage gates
are retained. No local .NET or tenant pass is claimed; final hosted results belong
in the reviewed PR. The no-admin Lists/Flow/browser pilots remain independent and
unchanged. No live practitioner record, scope, Entra app or CGov change.

Primary Microsoft guidance checked 25 September 2026:
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/register-plug-in
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/write-plugin-multiple-operation
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/best-practices/business-logic/include-filtering-attributes-plugin-registration
