# Implementation Readiness

Use this check to confirm the repo contains the handoff artifacts needed to move from source scaffold to live Power Platform build.
Local readiness checks only cover source artifacts, smoke coverage, and seed consistency.
Teams, Planner, and Office 365 Users connector placeholders are deployment blockers and not build blockers.
The app source contract check also keeps Teams, SharePoint, and Planner language deployment-gated so the Dataverse-first core remains build-safe.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-ImplementationReadiness.ps1
```

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-AppSourceContracts.ps1
```

The default mode fails only for core artifacts that should already exist. Optional live-build helpers are warnings until their implementation tracks land.

Use strict mode before a deployment work session:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-ImplementationReadiness.ps1 -Strict
```

Strict mode requires the Dataverse schema builder, flow implementation checklist, and app implementation checklist.
