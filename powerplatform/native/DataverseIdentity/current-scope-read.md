# Composed caller-bound current recorded-scope read

24 September 2026. N03.07/N03.08 source slice after Decisions#25 at
972ea7bbed5638dfe6708c0d554e9d09af180b75. No tenant deployment.

ReadCurrentRecordedScope is an actual IPlugin implementation that composes the
existing CallerResolver and VersionedScopeReader. It accepts only the bound record
Target, resolves the actual human/canonical-person mapping under caller access,
creates the read service for that same platform user and evaluates at server UTC
now. No client Actor, role, arbitrary evaluation time or source verification claim
is accepted. The resolver checks the actual identity index/mapping; read permissions
are enforced by the platform service, not by filtering the displayed fields.

The code supplies one planned API, dft_ReadCurrentRecordedScopeV1. No endpoint is
registered or permission granted by committing the source. The separate raw-record
read remains unchanged. The closed concurrent #26 is NOT a second merged reader;
this entry point uses #25's one version-guarded adapter. Both libraries belong to
Decisions; no sibling module, central runtime or new external package is required.
The test project now references the actual schema assembly through the module
project graph instead of recompiling a second copy of RegisterSchemaPlan.

The response distinguishes successful retrieval, existence of an effective
recorded decision, recorded status and verification currentness. Empty or future
history can be successfully read without showing a current grant. Restrictions
and supervision remain in the minimum-necessary selected decision. The result
always says recorded-data-only-not-practice-clearance; it is not authority for
rostering, access activation or a clinical competence finding. Private decision
papers, actor/source credentials and formatted SDK attributes are not returned.

Outputs start with only ReadSucceeded=false. Any exception in the operation clears
all outputs and raises the same generic error without its underlying message or
inner exception. A data/permission/identity failure cannot leak a previous successful
result or be interpreted as a refusal. There is no retry, write, message or privilege
fallback. The endpoint does not disclose whether an inaccessible record exists.
Operational support needs separately approved, access-controlled diagnostic logging;
this source deliberately does not log practitioner records or native fault details.

The deployment contract is source, not an importable managed solution or a security
role. Before registering: qualify dependent-assembly packaging, API execute privilege,
native response-property types including null SelectedDecision, actual row/column/
export permissions, complete identity mapping, immutable journal and native concurrent
read/storage fidelity. Matching root versions are change detection, not isolation
against privileged journal edits, or a guarantee that nothing changes after return.

Tests execute the composition with real SDK types and synthetic platform services.
They cover input tampering, identity failure, server time, scope/currentness, stale
outputs, native faults and composition without writes. Existing strict and locked
CI gates remain. This is not independent clinical/cyber acceptance or Dataverse
engine execution. No-admin Lists/Flow pilot remains unchanged and independently
usable under its existing synthetic-data boundary. No new user task, Entra app,
licence purchase, real credentialing decision or CGov change.

Microsoft primary sources:
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/impersonate-a-user
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/handle-exceptions
