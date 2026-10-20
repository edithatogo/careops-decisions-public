# Technology Stack

## Application Platform

- Microsoft Power Platform.
- Full Microsoft Dataverse in an approved dedicated Managed Environment is the
  mission-critical credentialing/register production target. Dataverse for Teams
  is not the production baseline for this capability.
- Model-driven Power Apps for register/governance views; focused custom pages or
  canvas surfaces only where user tasks justify them.
- Solution-aware Power Automate for small resumable workers and integrations;
  persistent cases/jobs/authority/decision state lives in Dataverse, not flow runs.
- Microsoft Teams is an optional application host and notification surface, not
  the authoritative credentialing record or source of delegated authority.

## Integration Surface

- Service-owned SharePoint libraries for controlled documents and versions;
  Dataverse retains stable references. Authorisation is tested in both services.
- Dataverse native backup/restore plus separately approved SharePoint recovery;
  Lists projections are read-only continuity aids, not a database backup.
- Personal OneDrive is excluded as an authoritative register or backup location.
- Outlook and Teams connectors for approved, deduplicated communications.
- Planner action synchronisation and Power BI reporting remain optional.
- Copilot Studio/AI Builder for bounded, review-gated preparation; AI is not
  required for the core service and cannot grant scope or change authority.
- Entra identity and approved Microsoft Graph/Azure capabilities only where
  tenant permissions, licensing and local approvals are established.

## Source and Automation

- Power Platform solution XML and PAC-compatible unpacked solution assets.
- Each core module has an independently installable managed solution; optional
  integration adapters must not force sibling/core business-table dependencies.
- PowerShell 7 scripts for validation, packaging, readiness and deployment.
- YAML/JSON contracts for application, flow, schema, reporting and test definitions.
- TypeScript/TSX only for supported generated/custom-page source where needed.
- Python reference kernels and the SQLite adapter are synthetic test evidence,
  not a production runtime. Native Dataverse parity tests are separate gates.
- GitHub Actions validates source and permitted package operations; it is not
  the CHHHS case-processing runtime.

## Quality Baseline

- `scripts/Invoke-RepoValidation.ps1 -RequireImplementationScaffolds` remains the
  repository-wide deterministic validation entry point.
- `scripts/New-ReleaseArtifact.ps1 -Unmanaged` must produce a packable solution
  before changes are considered release-artifact ready.
- Preserve strict reference typing/lint, full statement/branch coverage and
  negative tests. Do not remove tests when changing the production target.
- Dataverse identity, direct-write bypass, concurrency, rollback, access and
  cross-service recovery require executed sandbox tests, not SQLite inference.
- Tenant-dependent checks fail closed when enabled without required configuration.
- Hosted workflow success proves only checks that ran; skipped tenant checks do
  not establish deployment, security or operating effectiveness.
- Clinical, cyber, records, support, accessibility and migration acceptance remain
  separate requirements before operational activation.

## Dependency and Repository Policy

- Prefer Renovate after verified hosted installation; retain update coverage.
- Pin third-party actions to immutable revisions as CI hardening is completed.
- Keep solo-maintainer operation without mandatory human PR reviewers; enforce
  stable automated checks via authorised administration.
- Do not add a new register repository or mandatory parent/Atlas/AI runtime.

See [native register target](../docs/architecture/native-register-target.md).
This file records a design target; no environment has been provisioned by it.
