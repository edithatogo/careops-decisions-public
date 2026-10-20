# Transactional event journal for independent downstream work

24 September 2026. N03.05 source slice; follows Decisions#22 at
3cbab2f4d433339253ff4ab3e43d6f6b499a55d7. No operational activation.

## Native code supplied

`DecisionEventSchema.Build()` constructs ten SDK requests for a Decisions-owned
standard, user/team-owned event table: table, six columns, two internal restricted
relations and one unique event key. It does not execute them or install a trigger.
`JournalWithOutbox.Stage(context, service, intent)` calls the existing journal
adapter and records a minimal event using the SAME transaction-bound service.
The protected handler must authenticate and authorise the exact human decision
before invoking it; this lower-level wrapper is not an exposed endpoint.

A new append stages the decision, conditional root update and event in the
existing native transaction. No ExecuteMultiple/ExecuteTransaction request,
network delivery, compensating write or in-transaction retry is used. Any failed
operation must reach the platform and abort the whole transaction. Returning
StagedInTransaction is not proof of final commit. Actual Dataverse rollback and
storage fidelity must be exercised before deployment; fake-service tests prove
call ordering, validation and exception propagation only.

## Exact historical retry and restore boundaries

One event ID derives from one decision GUID. The envelope carries origin
organization, contract/version, register/decision references, sequence and the
original recording instant; it does not carry scope narratives, evidence papers,
practitioner details, recipients or an approval instruction. These identifiers
are still sensitive and need purpose-limited access, retention and auditing.

An exact old journal receipt is reconciled against its exact old event. Neither
is rewritten and no new event is generated. Missing, malformed, ambiguous or
inconsistent history is an explicit reconciliation failure, NOT a reason to
resend, invent a new GUID or recreate a deleted event. Any authorized backfill of
pre-outbox records is a separate reviewed migration. Restore/cutover must preserve
canonical event IDs and reconcile external delivery receipts before consumers
resume. Do not claim exactly-once email or external effects from a database key.

## Independent consumers, no additional staff queue

The immutable event is a durable handoff point, not a shared mutable task status.
Each enabled consumer must use its own versioned checkpoint/acknowledgement and
idempotency key. Notification failure must not block analytics, retry the human
decision, change a recorded scope or request another approval. Workers must fetch
only authorised data, recheck applicable current state and stop obsolete actions.
A recorded future-effective decision is not automatically current permission.

This slice intentionally installs no consumer, external connection, notification
or automatic applicant return. Those require bounded retry/timeout ownership,
dead-letter recovery, access and data-policy qualification. AI is optional. All
responses remain in the existing case workbench once that interface is connected;
there is no new user form or parallel approval queue.

## Release gates retained

The original journal-only staging method remains a low-level component, not a
production route to bypass the event invariant. The final protected handler must
use JournalWithOutbox for outbox-enabled records, with direct CRUD prevented.
ApplicationRequired metadata is not server enforcement. Verify table type,
actual active keys, transaction participation, permissions and immutable history
in an approved sandbox. Registration/managed solution packaging and the complete
authority-bound handler remain open. Native event availability is unverified.

The no-admin Lists/connector-free Flow pilot remains unchanged, needs neither
this table nor an Entra application registration, and stays synthetic only.
Full Dataverse remains the production target. No tenant, live record, delegated
authority, roster access, licence purchase or CGov contract was changed.

Microsoft primary guidance checked 24 September 2026:
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/scalable-customization-design/database-transactions
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/best-practices/business-logic/avoid-batch-requests-plugin
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/define-alternate-keys-entity
