# Native API definition for historical receipt recovery

25 September 2026. N03.06/N03.10 after Decisions#38 at
82039faabf4ea2d5bc1101471d1c838cb6624a43. Source only, no registration.

OwnDecisionReceiptApiDefinition.Build constructs the single native Custom API
and its typed contract for ReadOwnDecisionReceipt, not a competing reader or
write endpoint. It requires a stable API identifier, the exact inspected plug-in
type and an explicitly approved existing execute privilege. It validates row
shape only, not tenant provenance, source approval or assembly integrity.
No connection is opened, privilege invented/granted or SDK request executed.

The API is entity-bound to dft_credentialingrecord. The platform supplies Target;
the sole explicit input is required DecisionId (Guid). The nine response fields
match the actual reader's primitive output: record/decision IDs, original recording
time, sequence/outcome, stored case hash, retrieval time, interpretation and success.
The tests run the actual reader against the existing synthetic service and compare
its real output names/types to this definition, instead of comparing two examples.
The request and both child collections round-trip through SDK serialization.

This is an Action with read-only implementation, matching the existing platform
client convention. There are no optional identity, role, source, timestamp or
approval inputs. No extra processing steps or classic workflow invocation are
enabled. The existing caller/record/actor checks still apply. IsPrivate controls
discovery, not authorization; API state/status fields do not provide a supported
disable switch. No false disabled-default safety claim is made for this API.
The explicit execute privilege is not permission to read another person's receipt
or proof of delegated clinical authority. Native row/field security remains necessary.

## Use in the existing save experience

The future workbench should keep the original decision identifier across a lost
response and use bounded read recovery, not generate another approval/decision.
A recovered historical outcome must be labelled historical, never current scope.
A failed/empty read does not prove a failed save. Do not automatically resubmit,
repair events, change access or demand another clinician decision. Escalate only
unresolved technical reconciliation through the existing accountable process.
No new routine clinician queue, form or required click is added by this source.

## Acceptance still open

Inspect actual plug-in and privilege provenance, approved access, solution membership,
collisions, parameter/response metadata and Web API/SDK representations in an approved
sandbox. Reject drift rather than overwriting a known deployment blindly. Verify
caller/row/field security, access revocation between reads, failure output and
read-after-commit behavior. Factory/SDK tests are not native security or deployment.
The earlier reader documentation's registration gap is partly addressed by this
source definition; actual registration and workbench recovery routing remain open.

Use the existing no-admin Lists/Flow/fixed-browser pilots independently; they do not
require this full-app component. No new Entra app, tenant access change, practitioner
record, clinical scope, paid service or CGov arrangement is created. Full N03 remains
in progress. All existing locked dependencies, tests and coverage gates remain.

Primary Microsoft sources checked 25 September 2026:
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api-tables
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/customapirequestparameter
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/create-custom-api-with-code
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
