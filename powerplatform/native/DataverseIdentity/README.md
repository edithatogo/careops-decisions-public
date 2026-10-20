# Native Dataverse caller-to-person binding

24 September 2026. N02.07 source slice, resumed PR17 and reconciled with the
merged Decisions16/18 baseline. Programme: careops#160. Source only, not deployed.

The net48 SDK adapter reads the initiating platform account under that account's
permissions, cross-checks WhoAmI organization/user, rejects run-as mismatches,
disabled/non-read-write accounts and application identities, and maps the protected
directory GUID to Decisions-owned dft_orgperson. It never reads browser/agent
identity claims, guesses an email/name/title, elevates to SYSTEM, writes a record
or creates a person. Queries are bounded and a published Active single-column
identity index must exist before relying on caller-visible results.

The returned immutable mapping identifies an account/person. It does NOT prove
that a human is presently interacting, has confirmed a decision, or has delegated
authority. Delegated automation may operate with a user's connection. The final
write operation still needs a protected, exact-case human-confirmation and
current delegation check. Read-write accessmode is an account classification,
not proof of consent, attendance, clinical competence or authority. Applications
and background jobs need a separately approved service pathway.

The source fields are dft_orgperson/dft_aadobjectid; the required alternate key
is dft_orgperson_aadidentity. Actual index provisioning/activation, canonical GUID
normalisation, reconciliation and restricted identity-map maintenance remain
release gates. A unique index does not protect who may edit the mapping. Query
under the actual user's permissions, rather than broadening access on a failed
read. Unlinked applicants need not have an Entra account; this is actor resolution.
Use Person, not unrelated platform/Entra GUIDs, for actor/practitioner comparison.

Tests use simulated SDK responses, not Dataverse authentication or security.
The initial tests tried assigning server-read-only metadata properties and did
not compile. Test-only fixture reflection now supplies those internal-set values;
production uses only documented read properties. No runtime reflection or access
bypass was added. The SDK dependency graph is aligned with the already qualified
RegisterSchema net48 graph, with all-package auditing and warnings as errors.
Inspect and commit actual generated locks, then rerun locked restore before merge.

Missing identity, index or permission is an access/configuration exception, not
a practitioner deficiency or automatic revocation. No new staff review task or
queue is added. The no-admin Lists/Flow pilot remains independent: no Entra app
registration, app ID, tenant administration or Dataverse install was added to it.
This library is not registered or included as a live Custom API by this change.

Remaining gates: current signed CHHHS authority, protected mappings and occupancy,
actual native key/authentication/permission tests, exact-case confirmation,
transactional write host and clinical/cyber/operational acceptance. N02 stays open.

Primary sources checked 24 September 2026:
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/impersonate-a-user
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/systemuser
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/supported-customizations
