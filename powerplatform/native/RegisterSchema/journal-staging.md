# Native transaction-bound journal staging

24 September 2026. N03.04/N03.05/N03.06 source slice, stacked on Decisions#20
(4c4abc6b4954f4fa1afa75dc056be8189f6bd3b3). No tenant activation is included.

## What this layer does

RegisterJournalAppender.Stage uses actual Microsoft SDK request types and an
existing synchronous plug-in transaction. It accepts a complete DecisionAppend
from a SEPARATE protected handler; it is not an IPlugin, registered API, policy
resolver, human confirmation mechanism or proof that its caller has authority.
The future dft_CommitCredentialingDecisionV1 handler must authenticate and
resolve the exact human decision, signed delegation, case/evidence, canonical
actor, role occupancy/conflicts and current source versions before this call.
WhoAmI/context consistency only guards accidental service/run-as mismatch. It
does not prove human interaction or turn a supplied intent into an authorisation.

The adapter requires the correct synchronous Stage-30, direct, online,
non-impersonated transaction context. It makes a bounded exact-ID replay query.
An exact historical receipt returns AlreadyRecorded WITHOUT touching the latest
record: an old approval retry cannot overwrite a later restriction or revocation.
An existing but altered, missing-field or unverifiable receipt stops the action.
No failed retrieve is caught and reinterpreted as an absent record.

For a new receipt, the existing record must match the expected native row version,
explicit sequence and predecessor. A nonempty predecessor is also read and must
belong to that same record and sequence. The adapter first creates the immutable
history row, then advances the latest-decision pointer with UpdateRequest and
IfRowVersionMatches. The create precedes the lookup assignment because the new
referenced row must exist. A failed conditional update MUST abort the ambient
transaction including that create. No catch-and-continue, retries, batch request,
compensating delete, blind update or implicit record creation is implemented.

The only root fields written are latest sequence, latest decision and clearing
projection-as-of. Existing scope, restrictions, supervision and recorded state
are not overwritten by this journal layer. Invalidated projection currentness is
not a new approval or revocation; consumers must not treat stale projections as
clearance. StagedInTransaction does not mean committed: remaining handler work
and the native transaction can still fail after return. Zero is Unverified.

## Outstanding integration and native evidence

The complete protected write endpoint, authority/case concurrency guards, direct
CRUD prevention, active alternate-key/concurrency qualification, final projection
and durable event/outbox updates must be implemented and validated together.
This adapter is not a full atomic decision/history/outbox service. No endpoint or
production flow may expose it alone. Public handlers must sanitise and rethrow
errors to abort, not swallow a failed SDK operation and continue the transaction.

Exact persisted-value fidelity also remains a native acceptance gate. A null or
omitted restriction cannot be inferred to mean no restrictions; normalised time
precision is not guessed equivalent. Use an explicitly approved storage/receipt
representation and demonstrate actual round-trip/retry behaviour before enabling
writes. Existing-but-unverifiable receipts must not be recreated or overwritten.

Tests exercise the actual request objects against a deterministic fake service,
including call ordering, exact query columns, native CAS, same-record predecessor,
late retry, malformed/incomplete responses and faults at every SDK stage. They do
NOT establish real Dataverse transaction rollback, concurrency, authentication
or permissions. Execute those tests in an approved sandbox with appropriate
roles, injected conflicts, lost responses, downstream failures and restoration.

No new staff task, approval queue, application registration, package dependency
or sibling-module dependency is added. No live data or credentials were used.
The no-admin synthetic Lists/connector-free Flow pilot is unchanged and independent.
Full N03 remains open; only reviewed and tested source delivery can be closed.

Microsoft primary guidance:
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/optimistic-concurrency
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/best-practices/business-logic/avoid-batch-requests-plugin
- https://learn.microsoft.com/en-us/troubleshoot/power-platform/dataverse/plug-in-execution/dataverse-plug-ins-errors
