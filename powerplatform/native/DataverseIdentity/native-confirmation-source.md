# Acquire the original confirmation from native records

25 September 2026. N03.04-N03.06 after Decisions#46 at
685bb2a6eecbcff992324371497e64f5af9169af. No tenant installation.

ConfirmationSourceSchema constructs a Decisions-owned standard source table with
13 controlled columns, a restricted-delete relation to the existing register and
an alternate key. No attachment, activity or sibling-module table is required.
The table is the original source for an existing human decision, not an additional
clinical approval register or a form for staff to complete twice. All custom
content columns request auditing and field security. Default withdrawn=true and
outcome=Unverified cannot stand in for a captured decision. ApplicationRequired
metadata is not server-side integrity or a substitute for native access controls.

NativeConfirmationSource.Read uses the SAME CallerBinding service to retrieve
one canonical source row with a positive native row version. It checks current
native access, creator systemuser, canonical person, exact record/source ID and
capture protocol, inactive/withdrawn/missing states, original whole-content digest,
original evidence reference, outcome/function and exact UTC confirmation period.
It restores the saved owner-bound DecisionAppend, not a document newly assembled
from the current case. The returned source is immutable and contains no SDK entity.
Native faults and malformed source data produce a generic error without details.

The exact original confirmation instants are stored as UTC round-trip strings,
not reliant on native DateTime storage retaining every fraction. The full original
intent remains in the bounded versioned document. Actual native text, Unicode,
empty-string, metadata and permission behavior still require approved sandbox tests.

## Source authenticity and concurrency are separate gates

A native creator, protocol string, captured digest and readable row DO NOT by
themselves establish an actual approved human action. A controlled capture endpoint,
direct Create/Update restrictions, independent approved outcome-policy resolution,
correct ownership and immutable evidence fields are required BEFORE this adapter
is used in a production writer. No such endpoint is registered by this code.
Source fields must not be editable to manufacture a confirmation or change the
original pathway/function. No client/agent-provided trusted snapshots are accepted
by this reader, but privileged source tampering is not solved by parsing.

This is a source READ, not a transaction lock. RowVersion and FenceToken are observed
values, not authority, a lock receipt or protection against concurrent withdrawal.
A following transaction-bound concurrency fence and independent native authority/
occupancy source controls must protect the recording interval. Re-reading alone
cannot close that gap. These requirements remain open after this source slice.
The source's policy reference still requires the actual approved rule version;
it is not interpreted as policy approval by this reader.

The original creator must be the current verified human. On-behalf creation is
rejected; administrative recording on behalf needs a separately governed actor/
recorder pathway, not impersonation or a repeated clinical approval. Historical
receipt recovery remains separate and must not try to restore expired sources
or resubmit an old decision. An unavailable original source is not an adverse
finding about the practitioner and does not change any existing scope.

Tests execute real SDK metadata, CallerResolver and source/intent restoration
with synthetic responses. They test canonical identities, source binding, dates,
missing fields, strict defaults, row versions and immutable results. No local .NET
or native tenant success is claimed; inspect final hosted artifacts. Existing
dependencies and full named-library coverage gates remain unchanged. No-admin
M365/browser pilots remain independent; no tenant, app ID, privilege, clinical
scope, real record, paid service or CGov change. Full N03 remains in progress.

Primary Microsoft documentation checked 25 September 2026:
- https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.memoattributemetadata.maxsupportedlength
- https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.booleanoptionsetmetadata
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/column-level-security
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/optimistic-concurrency
