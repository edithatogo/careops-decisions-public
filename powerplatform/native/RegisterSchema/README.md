# Native Dataverse credentialing register schema

24 September 2026. N03.02/N03.03 source slice; predecessor Decisions#16,
merge 8bf56acbc16d2add7f27f75c94e6e66a88656479. Full N03 remains open.

## Delivered interface

`RegisterSchemaPlan.Build()` returns ordered Microsoft SDK metadata requests for
two standard, user/team-owned tables under the existing DecisionsWorkspace/dft
publisher. It has no service client, network call, command-line installer,
credentials or record input. Constructing requests does not execute them.
`InspectionRequests()` reads published native metadata when an approved installer
later executes those requests. It does not assert that tables exist today.

| Table | Owns |
| --- | --- |
| dft_credentialingrecord | Stable canonical record/person/profession/facility references and current operational projection, including conditions and supervision. |
| dft_credentialingdecision | Ordered human-decision history, source/evidence/profile bindings, validity/effect/recording instants and explicit supersession. |

There are 42 creation requests: two tables, 34 columns, three internal relations
and three alternate keys. Primary name fields contain record identifiers, not
personal names. No sibling module tables or business lookups are required.
Practitioner/facility/profession/source references are controlled stable IDs, not
another authority database. Canonical key values must be server-generated and
normalised; arbitrary external identifiers belong in source-reference fields.

Both tables request auditing. Evidence/actor/profile details in the decision
table request field security; operational projection does not copy those fields.
Field security is not a substitute for approved row/team roles, API/export/search
checks or access to linked SharePoint evidence. No security role is granted here.
Business-required metadata is NOT a server-side integrity constraint; the future
protected write API must reject missing/altered values and direct CRUD bypasses.

## History, currentness and conservative defaults

Recorded state defaults to Unverified, never Granted. These are stored decision
states, not preparation RAG scores or permission to practise. Future projection
logic must retain revoked/suspended history and evaluate effective/validity times,
source currentness and approved conditions. The latest recorded decision may be
future-effective; its pointer must not be mistaken for the current decision.
Projection-as-of and projection-sequence remain separate from latest sequence.

UTC instants use native DateTime/UserLocal semantics, with display conversion by
the platform. Civil-date counting and local policy boundaries still need approved
calendar/timezone conversion in the protected host. Unknown source verification
has nullable timestamps, not a fabricated fresh/green state.

Relationships restrict deletion and do not cascade sharing, ownership, merging
or reparenting. History is not thereby append-only: immutable business events,
anti-replay, exact delegated authority, same-record supersession, atomic writes
and permissions must be enforced by the protected API and verified in Dataverse.
No stored actor reference, hash, status, list indicator or form field proves an
actual human decision. Primary-source and signed authority work remains open.

## Native qualification and install gate

The library compiles against official Microsoft SDK assemblies on .NET Framework
4.8. Tests examine native metadata and data-contract round trips, ownership,
keys, deletion/sharing rules, timestamps, bounds, state defaults and fresh-object
isolation. CI requires full line/branch coverage, no skipped/empty tests and
warnings-as-errors. Initially generated dependency locks must be inspected and
committed before the final locked-restore qualification and merge.

This is NOT yet a managed solution export or a clean Dataverse installation.
Before executing: verify existing solution/publisher and name collisions, choose
an approved empty sandbox, verify required customization privileges, import
through reviewed ALM, then read published metadata. Check actual table type,
optimistic concurrency, column security and active alternate-key indexes; key
creation is asynchronous and null key values do not provide uniqueness. Perform
real duplicate/race/rollback/access/restore tests before operational use.

Do not overwrite an existing table, delete records on an import conflict, or run
these requests repeatedly as an implicit migration. The future installer needs
explicit drift detection, resumable checkpoints and an approved upgrade plan.
No privileged CLI, app registration or tenant provisioning is run by this PR.
The no-admin Lists/Flow pilot remains separate and unchanged; operators need not
install this full-app development component. No CGov or live register change.

## Primary sources checked 24 September 2026

- Microsoft SDK metadata sample: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/samples/create-update-entity-metadata
- Table creation: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.messages.createentityrequest?view=dataverse-sdk-latest
- Relationships: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.messages.createonetomanyrequest?view=dataverse-sdk-latest
- Alternate keys: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/define-alternate-keys-entity
- Optimistic concurrency: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/optimistic-concurrency
- SDK package: https://www.nuget.org/packages/Microsoft.CrmSdk.CoreAssemblies/9.0.2.60
