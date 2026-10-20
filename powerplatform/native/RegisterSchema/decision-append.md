# Exact native decision append intent

24 September 2026. N03.04/N03.06 source slice following Decisions#19.

DecisionAppend freezes the complete evidence-backed decision supplied by a
protected host. It is a storage value, NOT a verification receipt, signature,
authority check, human confirmation or callable API. Never bind browser/agent
fields directly to this constructor and treat successful construction as approval.
The host must resolve current signed delegations, authenticated canonical actor,
exact case/evidence versions, professional review and human confirmation first.

Every business field is explicit. Missing restrictions/supervision cannot become
an empty value. An empty string means the host has explicitly verified absence,
not that a field was omitted or inaccessible. Unsupported/unverified decision
states, invalid hashes, ambiguous timezones, excessive values, malformed Unicode,
unknown fields and impossible sequence/predecessor combinations are rejected.
Validity is a nonempty UTC interval for the associated scope; recorded/effective
instants are separate. No clinical duration or legal calendar convention is guessed.

Stable decision GUIDs derive canonical storage identifiers. Expected row version
and last sequence bind the upcoming compare-and-swap against an EXISTING record.
No implicit record creation or blind last-writer-wins update is provided. Fresh SDK
entities and query-column lists cannot mutate the frozen intent. Exact replay
comparison covers all persisted business fields, including constraints, evidence,
predecessor and state. Matching a prior receipt is never permission to reapply an
old grant over a later restriction or revocation.

This slice makes no SDK calls or writes. The next adapter must use an existing
native transaction, caller-scoped service and conditional UpdateRequest with
IfRowVersionMatches. Do not put ExecuteTransactionRequest/ExecuteMultipleRequest
inside a plug-in. Do not catch a failed data operation and continue. Actual native
race/rollback/idempotency tests, protected write endpoint, direct-CRUD enforcement,
outbox, projection refresh and operational acceptance remain open. Metadata
ApplicationRequired is not a server-side integrity control.

Source tests execute with the existing locked native register-schema suite and
whole-library coverage requirement. They do not execute a Dataverse tenant.
There is no new package, licence, app registration or sibling dependency. The
no-admin Lists/Flow rehearsal is unchanged and does not require this full-app code.

Microsoft primary guidance checked 24 September 2026:
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/optimistic-concurrency
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/best-practices/business-logic/avoid-batch-requests-plugin
- https://learn.microsoft.com/en-us/troubleshoot/power-platform/dataverse/plug-in-execution/dataverse-plug-ins-errors
