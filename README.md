# CareOps Decisions — public source

Clean-history source publication with fictional deployment bindings. Private
history, issues, operational evidence and actual tenant identities are excluded.
All included deployment profiles disable live writes. Historical implementation
notes describe source context, not a deployed or accepted public installation.
Provide private target settings explicitly before any separately authorized use.


[![Validate](https://github.com/edithatogo/careops-decisions-public/actions/workflows/validate.yml/badge.svg)](https://github.com/edithatogo/careops-decisions-public/actions/workflows/validate.yml)

CareOps Decisions Workspace is an organisation-neutral, Microsoft
365-integrated governance system for committees, boards, working groups, and
recurring operational forums. It manages terms of reference, membership,
quorum, conflicts, agendas, papers, attendance, minutes, recommendations,
formal decisions, actions, transcripts, reporting, and audit evidence.

Current version: `1.0.0`

Licence: Apache-2.0.

## Architecture

The repository is repo-first and solution-aware:

- Conductor context and tracks under `conductor/`;
- Power Platform source under `powerplatform/`;
- architecture, operations, security, and deployment documentation under
  `docs/`;
- synthetic fixtures and smoke tests under `tests/`; and
- environment-specific identities, connections, and secrets outside source
  control.

The first implementation path uses Dataverse or Dataverse for Teams, Power Apps,
Power Automate, Teams, Outlook, Planner, SharePoint, and approved AI Builder or
Copilot capabilities. Full Dataverse, Entra groups, Microsoft Graph, Azure
services, Power BI, and Fabric remain optional enterprise extensions subject to
separate readiness gates.

## Modular capability packs

The stable governance core is reusable across domains. Domain-specific features
are added as disabled-by-default capability packs with their own schema,
workflow, security, testing, deployment, and rollback evidence.

The first new pack is the credentialing governance extension:

- committee mandate, membership, quorum, proxies, and conflicts;
- immutable case-packet receipt from `careops-approve`;
- relevant-peer and service-capability evidence references;
- procedural-fairness status;
- committee recommendation and reasons;
- separate delegated decision and authority reference;
- conditions, supervision, commencement, and expiry;
- review and appeal references; and
- privacy-minimised event handoff to `careops-process`.

See
[`docs/architecture/credentialing-governance-extension.md`](docs/architecture/credentialing-governance-extension.md).

The extension does not make clinical or credentialing decisions. AI may draft
minutes, summaries, recommendations, reasons, and actions, but every generated
record remains draft until an accountable human reviewer accepts, edits, or
rejects it. Recommendation and delegated decision remain separate human acts.

## Build surface

- Conductor tracks: `conductor/tracks`
- Power Platform source scaffolds: `powerplatform/src`
- PAC solution project: `powerplatform/solutions/DecisionsWorkspace`
- Deployment settings sample: `powerplatform/settings/deployment-settings.sample.json`
- Smoke tests: `tests/smoke`
- Power Apps Test Engine skeleton: `tests/powerapps`

The repository contains a validated governance-workspace baseline. Live
connector binding, environment configuration, deployment, and operating
effectiveness remain separate gates.

## Validate

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-AlmLayout.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-SourceArtifacts.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Invoke-RepoValidation.ps1 -RequireImplementationScaffolds
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Get-EnvironmentReadiness.ps1
```

No patient, practitioner, workforce, credential, committee-paper, production,
or secret material belongs in this repository. Source control contains code,
contracts, schemas, synthetic fixtures, and non-sensitive implementation
documentation only.

## Exact source-package evidence

Validation inventories the actual unmanaged solution ZIP already packed by the
existing source workflow, checking its canonical identity and unmanaged mode.
Main-only OIDC jobs download the same archive, compare all file digests against
SPDX, sign provenance and inventory, and verify exact source/workflow identities.
Artifacts remain private and no release, import or deployment is performed.

This qualifies source-package evidence only. It does not establish native-module
inclusion, runtime behaviour, operational acceptance or solution-checker access.
The four native test/coverage lanes and existing optional checker retain their
own gates. Hosted Windows execution and signature availability remain unverified
when account billing refuses jobs before they start.


## Native library release candidate

The native release workflow builds the four existing library/test projects as
one locked graph, packages project-owned CLR assemblies, API XML, the project
licence and exact source, then executes all four existing suites against the
unpacked package bytes. Real test totals and source identity are retained.
Build and SPDX signatures are produced only after successful main qualification.

The archive supports offline library/source consumption on Windows .NET
Framework4.8. Third-party dependencies remain in the committed NuGet locks;
they are not bundled or represented as complete binary dependency closure.
Dataverse registration, solution integration and operator acceptance remain
separate from this library release. The existing unmanaged scaffold evidence
is retained and is not used as proof that the native libraries shipped.


## Modular source delivery and ALM

Follow the shared delivery policy and ordered source/native queue in
[the module profile](.careops/module.json). Prefer exact useful development pins,
isolated qualification and retained recovery over replacing a working runtime.
Preserve existing publisher/logical names, stable IDs, connections, schedules and
current owners. Deduplicate only with component identity and dependency evidence.

Keep native exports, target bindings and operational records private. Version
external artifacts that do not belong in a Dataverse solution through their
existing deployment manifests. Record actual solution-layer and pipeline/Git
prerequisites separately from source CI; this profile configures no tenant.
SharePoint structured document generation remains the preferred route for any
selected structured report or document, subject to exact entitlement, complete
output, human-edit preservation and recovery qualification.
