# Read an existing own decision receipt without repeating the decision

25 September 2026. N03.06/N03.07 source after Decisions#37 at
8034a7ce5ba62205e868ffbdbb8d48f0c5441e04. Not registered in a tenant.

ReadOwnDecisionReceipt is an actual IPlugin for a separate read-only, record-bound
operation. It accepts only the saved record Target and stable DecisionId. It uses
the same CallerResolver binding and verified caller-scoped service as the writer.
A current account, canonical person mapping and current read access to the root
and original decision remain necessary. Current permission to APPROVE again is
not needed simply to read a historical decision made by that same person.
There is no privilege elevation or service-account fallback.

This addresses lost responses and retries after the original delegation or human
confirmation has expired. It never invokes HumanConfirmedJournal or another writer,
resubmits events, repairs history, repeats an approval or selects a new decision ID.
Return only the original ID, sequence, recorded outcome/time, saved case hash and
retrieval time, with the fixed historical-only disposition. No evidence documents,
clinical narratives, owner assignments or delegation records are disclosed.
The seven-column query is bounded to two results and matches the canonical person,
record and decision. These bindings are independently checked on the returned row.

Root access is checked before fetching the decision AND immediately before emitting
the validated receipt. Root and decision sharing are not assumed to cascade.
The second access check uses the same caller service and rejects revoked access or
changed/malformed root identity. The receipt primitives are frozen before this
final call, preventing mutable SDK objects from altering validated output. This
is bounded revalidation, not an atomic permissions snapshot or a promise that an
administrator cannot subsequently change access. Native permission-change behavior
still needs target-environment testing; no per-case user task is added.

## No false certainty on either branch

A found receipt proves only that this accessible historical row was read. It is
NOT the currently effective scope, not proof of ongoing source currentness, not
an attestation that a later edited request matches the whole confirmed content,
and not a delivery acknowledgement for downstream event consumers. CaseHash is
one stored field, not the full DecisionConfirmationHash. Use the existing current
scope read for current recorded status. Never revive an earlier grant from a receipt.

No row, access denial, ambiguous results, malformed fields or service failure all
produce the same technical failure, clear earlier response output and perform no
write. An unavailable receipt does NOT prove the save failed. Do not automatically
create a replacement decision or ask the clinician to repeat approval. Controlled
reconciliation, with the original stable decision identity, remains the fallback.
If a user no longer has record access, do not bypass that restriction to recover it.

The intended workbench can use this as a bounded recovery step in the existing
save experience, not another mandatory screen or review queue. UI routing, native
Custom API definition/registration, execute privilege and wire-format qualification
are not supplied by this slice. Never run unlimited polling from a technical error.

## Qualification and remaining native controls

SDK tests exercise the actual IPlugin/CallerResolver with synthetic platform responses,
minimal query construction, no external calls/writes, account/record/actor matching,
old outcomes, malformed results, input mutation, access revocation and output cleanup.
The test fixture uses IPluginExecutionContext to represent missing collections;
it does not attempt assignment to RemoteExecutionContext's read-only properties.
Full named-library coverage and every existing test/locked dependency/analyser gate
remain. No local .NET run, tenant permission check or real transaction test is claimed.

The read assumes the append-only records and authoritative creation path are properly
enforced in the target environment; it does not validate a whole decision history,
source authenticity, event completeness or native storage precision. Current write
source-adapter concurrency, actual roles/field security, read-after-commit behavior,
native deployment, retention/recovery and workbench acceptance remain open.

This is owned entirely by Decisions. No other CareOps module or AI service is needed.
The no-admin Lists/Flow/fixed browser pilots are unchanged and remain independent.
No new Entra application, tenant privilege, real record, clinical scope, paid service
or CGov arrangement is changed. Parent N03 remains in progress.
