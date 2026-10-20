# Caller-scoped credentialing record read

24 September 2026. N03.07 source slice following Decisions17 merge
4f8f9d1505a605d0ece954f9a91bb367a5d81be0. Full N03 remains in progress.

## Implemented native operation

`ReadCredentialingRecord` implements Microsoft's IPlugin for the main operation
of the versioned, entity-bound `dft_ReadCredentialingRecordV1` Custom API. The
source contract declares the request/response and registration requirements; it
is NOT an importable managed solution or proof the API exists in a tenant.

This route accepts one exact record Target and rejects caller-supplied roles,
column lists, alternative table identities and arbitrary identity claims. It
requires direct synchronous main-operation context, resolves the caller through
the Decisions identity component, and retrieves only 16 operational projection
fields through the caller's own IOrganizationService. No SYSTEM fallback,
privilege grant, retry, network endpoint setting or external connector is added.

The result is newly assembled from an allowlist, not an unfiltered SDK entity.
It excludes decision/authority papers, unexpected attributes, formatted values,
related entities and alternate keys. It retains the retrieved row version and
source/currentness/projection fields without inventing a refreshed source check.
Wrong table/record, missing row version, malformed types and oversized text fail.
Expected access/SDK/time-out faults are replaced with a non-disclosing generic
message. No successful or partial record is emitted before validation completes.

## Consumer meaning and workload

`ReadSucceeded` means ONLY that this recorded-data read completed. The fixed
`ReadDisposition` is `recorded-data-only-not-practice-clearance`. A recorded
Granted value is not an assessment of current authority, validity or suitability.
Revoked values and stale source timestamps are preserved; nothing is approved,
extended, revoked or written by the reader. `RetrievedAtUtc` is retrieval time,
not source-verification time or the date an approval took effect.

Missing/null fields are listed explicitly in `UnavailableFields`; this may mean
missing or access-filtered data. Do not interpret an unavailable condition or
supervision field as no restriction. Empty stored text is returned literally,
not certified as no conditions. Render all returned text safely as text, not HTML.
A consumer must require explicit success, expected disposition and record identity,
then apply the separate approved current-scope logic. A default response when a
plug-in is unregistered is NOT success. Native row/version serialization also
needs SDK and Web API acceptance tests before a workbench uses it operationally.

This supplies the planned single workbench/register view; it creates no second
application, review queue, approval step or form for staff. Technical access
failures go to support, not to the practitioner as a credentialing deficiency.

## Registration and security gates still open

The native contract specifies Entity binding with the platform-generated Target,
Action semantics, no extra processing steps and noncustomizable managed API and
response definitions. Classic WorkflowSdkStepEnabled is false because Entity and
StringArray responses are not classic-workflow parameter types. Setting IsPrivate
hides metadata discovery; it is NOT a security control. Resolve an actual existing
read privilege, register the exact plug-in type as main operation and verify the
configuration in an approved sandbox; do not create an extra PostOperation step.
No runtime registration, signing/package deployment or privilege setup occurs here.

Identity resolution is NOT live human confirmation. A delegated automation can
use a user connection. This is a recorded-data read only; decision writes still
require a protected exact-case confirmation, current delegation and transaction.
Actual native row/team/column permissions and purpose/profession/facility access
must be approved and tested, including direct API/search/export. This handler
does not make an otherwise over-broad role safe, and must not be the sole way
sensitive evidence is protected. Directory mappings/index, table metadata and
read-only account/service routes need their own qualification. The current
limited route uses the already implemented enabled read-write account binding.

Tests compile actual IPlugin/SDK code against simulated services. They are not
Dataverse's security engine or a tenant registration test. The existing locked
native-identity workflow covers the entire combined assembly at 100% line/branch
coverage, with no skipped/empty tests and warnings as errors. Schema/contract
consistency tests compare the read fields with the current SDK schema source.
No new NuGet package or sibling runtime dependency is added.

The no-admin Lists/connector-free Flow pilot is untouched and does not install
this full-app component, need Dataverse, or gain an Entra app ID requirement.
No real register records, signed authority, tenant permission or CGov contract
has been changed. The protected write path and native deployment remain open.

## Microsoft primary documentation checked 24 September 2026

- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api-tables
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/entity-operations-retrieve
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/best-practices/work-with-data/retrieve-specific-columns-entity-via-query-apis
