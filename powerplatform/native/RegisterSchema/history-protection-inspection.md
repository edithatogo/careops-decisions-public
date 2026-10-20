# Verify observed history-guard registrations without changing them

25 September 2026. N03.04/N03.10 source after Decisions#34 at
b5f95396fa06356cc4473dd64d2e025d0a264a15. Not executed in a tenant.

HistoryProtectionInspector.Inspect is a bounded read-only SDK component for an
approved deployment check, not a per-case review or a public approval endpoint.
It uses a supplied approved connection and verifies WhoAmI against the expected
organization/principal. It does not create a connection, request an app ID,
elevate permissions or infer that the connection principal is a clinical delegate.
The expected plug-in type must already be bound to a reviewed deployment artifact.
This check cannot establish assembly integrity or metadata provenance on its own.

The inspector queries all registrations for that type, without hiding disabled
steps or filtering by a desired table/message. Exactly four unique active bindings
must form decision/event x Update/Delete. Missing, duplicate, extra, broader,
asynchronous, incorrectly ordered or disabled definitions fail. Native handler,
message and filter metadata are read back and checked against the same reviewed
factory used to construct registration. That factory's local disabled definition
is NOT treated as proof of installed active protection.

It also checks for forbidden filtering/configuration/impersonation settings and
unnecessary images. These existence queries return identifiers only: potentially
large or sensitive configuration contents and image data are not retrieved.
The successful return is an immutable list of observed step IDs, not a reusable
permission token, signed safety approval or readiness of N03 as a whole.
A denied read, unknown field or SDK error produces one generic technical failure;
there is no enabling, repair, retry, suppression of a safeguard or practitioner
message. Use existing release/technical processes, not a new clinician queue.

## Evidence limits and required native tests

The metadata reads are bounded but are NOT one atomic snapshot and do not prevent
an administrator changing registration after inspection. Run under approved
metadata-read access, retain the environment/artifact/time/test provenance in the
controlled deployment receipt, and execute actual prohibited-operation tests.
Native assembly identity, current signed delegations, creation/write authority,
blocked bulk/Upsert/state/assignment/deletion operations, administrative bypass,
retention and recovery remain separate acceptance requirements. Do not use a
positive metadata observation to waive them or present a disabled build as a
working guard. Restricted roles and change-control/drift monitoring remain vital.

The SDK/fake-service tests assert exact query bounds and the complete matrix,
missing/coerced states, identity/filter mismatches, extras, secret-minimizing
existence checks, read-only behavior and generic failures. They are not real
Dataverse registration, pipeline or security tests. No local .NET/tenant pass is
claimed; hosted final-head tests and review must pass before source merge.

No runtime call is added to the register card or an individual credentialing
application. All components remain owned by Decisions; no sibling module is
required. The no-admin Lists/Flow/fixed-browser pilots are unchanged and do not
need this full-app deployment check. No tenant, app registration, data, clinical
scope, paid service or CGov arrangement was changed. Parent N03 remains open.

Primary Microsoft sources checked 25 September 2026:
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/sdkmessageprocessingstep
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/sdkmessageprocessingstepimage
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/queryexpression/overview
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/bypass-custom-business-logic
