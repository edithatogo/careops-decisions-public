# Native definition for the existing current-scope read API

24 September 2026. N03.07/N03.10 source follow-through after Decisions#27
(b9ebc520a3e50bd16cf5fced6586191770f48282). No tenant registration performed.

CurrentScopeApiDefinition.Build prepares one Microsoft SDK CreateRequest for the
existing ReadCurrentRecordedScope handler, with all ten response-property records
in the same deep-create definition. Binding is to dft_credentialingrecord; Target
is platform-generated rather than an extra user input. No actor, role, date or
source-verification parameters are introduced. Definition and response names are
tested against both the actual IPlugin output and tracked source contract.

The builder rejects a missing/different plug-in type, absent canonical row IDs,
alternate-key references and blank/malformed execute privileges. Callers provide
previously inspected plugintype and privilege rows from the approved target. It
does not invent a privilege, grant a role, choose a service account or retrieve
credentials. Microsoft permits requiring an existing privilege; its suitability
and assigned roles require local approval. Valid-looking Entity objects are not
provenance: verify the package, plug-in ID/type and privilege through the native
installer before executing anything. No privileged installer is supplied here.

No extension steps or classic workflow use are enabled. IsPrivate=true hides
metadata; it does NOT prevent invocation or replace execute/row/column security.
Managed customizability is false so the reviewed API shape is not silently changed;
this does not restrict ordinary approved delegation-profile edits. ReadSucceeded
must still be explicitly true, and neither that nor a default response is clearance.
No real register or practitioner data belongs in definition-generation inputs.

The returned SDK objects are fresh, intentionally inspectable metadata. They must
not be treated as signed or immutable deployment authority. The approved ALM path
must inspect collisions, package dependencies and the complete definition, then
perform a controlled sandbox create/import and read-back. A returned request is
not a managed-solution ZIP or proof of native registration/transaction success.
Do not rerun creates as blind upgrades, remove execute privileges to fix access,
or register an API without its handler. The installer must record real generated
component IDs and export them through the versioned solution lifecycle.

Remaining native gates include the nullable SelectedDecision response, SDK/Web API
round-trip, restricted identity/index access, permission-denied reads, source
storage fidelity, concurrency and module-only package installation. The no-admin
Lists/connector-free Flow pilot requires none of this full-app registration and
remains unchanged. This adds no clinician application or approval step.

Primary Microsoft documentation checked 24 September 2026:
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/create-custom-api-with-code
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api-tables
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
