# Keep recorded decisions and their events append-only

25 September 2026. N03.03/N03.04 source after Decisions#33 at
2c8703ec1a6d7da07e6930c64d2453e08d2860eb. No native deployment.

RejectHistoryMutation is a terminal IPlugin: every invocation throws a fixed,
non-identifying InvalidPluginExecutionException. It intentionally does not read
services, input flags, role names, old record values, depth or a claimed exemption.
The native registration, not caller-controlled data, limits this unconditional
guard to Update/Delete on the two final-history tables. It must not be registered
on Create, on operational register updates, or on other modules' tables.

HistoryMutationStepDefinition builds each exact disabled registration from
inspected plugin/message/filter rows. The required deployment matrix is:

| Table | Operation | Stage / mode |
| --- | --- | --- |
| dft_credentialingdecision | Update | PreValidation / synchronous |
| dft_credentialingdecision | Delete | PreValidation / synchronous |
| dft_credentialingevent | Update | PreValidation / synchronous |
| dft_credentialingevent | Delete | PreValidation / synchronous |

All four need separately qualified, enabled registrations. There are no images,
filtering attributes, impersonation or data reads. Unknown, mismatched or broader
metadata is rejected. The factory builds SDK objects only; it does not connect,
register, activate or certify native metadata provenance. Disabled source is not
active protection. Required guards left disabled cannot pass production acceptance.

## Corrections, staff workload and independent workflows

Final decisions are corrected through an authorised appended superseding decision,
not an edit to the original grant or revocation. The event journal is immutable;
each consumer keeps its delivery/checkpoint state separately. A notification retry
must neither edit the original event nor ask for the decision again. The operational
register and draft workflow stay distinct from these final rows.

This terminal guard also blocks otherwise harmless repeated Update requests and
administrative updates to these immutable rows. Set approved ownership at creation;
maintain access through separately controlled roles/team membership, not by assuming
an in-place ownership migration remains available. Upsert must not be the retry
mechanism: the existing journal checks a matching old receipt without writes.
No extra clinician step or queue is added to the ordinary path.

A records-management/legal-hold/disposal process is a separate release requirement.
Blocking application-level deletion is NOT an instruction to retain personal data
forever, nor permission to override a lawful disposal duty. That controlled lifecycle
and any legacy migration need approved, audited designs before operational use;
this source offers no generic bypass flag or automatic deletion tool.

## Native acceptance still required

A synchronous PreValidation rejection normally avoids database work; nested calls
can already be in a transaction. Actual single, nested, bulk, Upsert, state-change,
assignment and deletion/cascade behavior must be exercised in an approved sandbox.
Read back exact active bindings and audit bypass privileges. CanBeBypassed=false
is requested metadata, not proof against platform administrators or bypass rights.
Metadata consistency, admin controls, backups and immutable evidence are separate.

This does not authorize Create or prove human confirmation, current delegation,
source authenticity, transaction rollback or lawful retention. The complete
protected writer, native operations tests and source/role approval remain open.
No-admin Lists/Flow/fixed-browser pilots stay independent and unchanged. No tenant,
new Entra app, clinical scope, practitioner record or CGov arrangement is changed.

Tests execute the actual handler and official SDK metadata construction/serialization
with synthetic inputs, using existing locked dependencies and coverage gates.
Final-head hosted qualification is required before merge; no local .NET pass claimed.

Microsoft sources checked 25 September 2026:
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/handle-exceptions
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/register-plug-in
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/write-plugin-multiple-operation
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/bypass-custom-business-logic
