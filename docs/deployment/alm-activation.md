# ALM Activation

CareOps Decisions is ALM-ready and has a live unmanaged solution baseline in the target Power Platform development environment.

Target environment details:

- Environment name: Dylan
- Account: operator@example.invalid
- Environment URL: https://environment.example.invalid/
- Live solution: `decisionsworkspace` / `CareOps Decisions` 1.0.0.0 unmanaged
- Latest import/publish: completed on 2026-05-03
- Azure/Entra service-principal implementation: deferred until tenant access and approvals exist

## Repo-Side Pieces Now Present

- Validation workflow: `.github/workflows/validate.yml`
- ALM profiles: `powerplatform/settings/alm-profiles.json`
- Connection reference manifest: `powerplatform/settings/connection-references.json`
- Environment variable manifest: `powerplatform/settings/environment-variable-manifest.json`
- Solution checker wrapper: `scripts/Invoke-SolutionChecker.ps1`
- ALM activation validator: `scripts/Test-AlmActivation.ps1`
- Local Dylan settings generator: `scripts/New-LocalDeploymentSettings.ps1`

## Dylan Activation State

The current Dylan connection inventory supports a partial local settings file:

- Bound: Dataverse, SharePoint, Office 365 Outlook, AI Builder Dataverse reference.
- Missing: Microsoft Teams, Planner, Office 365 Users.
- Still required: real `decisions_TeamsTeamId`, `decisions_TeamsChannelId`, `decisions_PlannerPlanId`, and `decisions_SharePointSiteUrl` values.

Generate the local file after connector changes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-LocalDeploymentSettings.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Get-EnvironmentReadiness.ps1 -TargetProfileName development -DeploymentSettingsPath powerplatform/settings/deployment-settings.local.json
```

The local file is ignored by Git because it contains tenant and user-specific connection IDs.

## Live Activation Steps

1. Confirm the Dylan development environment URL.
2. Authenticate with `pac auth create` or select an existing profile.
3. Confirm or create publisher `DecisionsWorkspacePublisher` with prefix `dft`.
4. Package and import the unmanaged solution with `scripts/Import-Solution.ps1 -TargetProfileName development -PublishChanges`.
5. Create or repair missing Teams, Planner, and Office 365 Users connections in the Dylan maker environment.
6. Generate `powerplatform/settings/deployment-settings.local.json` and confirm there are no placeholder warnings.
7. Import settings with `scripts/Import-Solution.ps1 -TargetProfileName development -SettingsFile powerplatform/settings/deployment-settings.local.json -PublishChanges`.
8. Activate app surfaces, flows, and security roles incrementally.
9. Export and unpack live changes back into `powerplatform/solutions/DecisionsWorkspace`.
10. Run solution checker and repo validation before pushing.

## Deployment Evidence

Every live Dylan deployment or verification attempt should leave a timestamped evidence bundle under ignored `artifacts/deployment/` output:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-DylanDeploymentEvidence.ps1 -PacTimeoutSeconds 30
```

The evidence wrapper runs the read-only Dylan verifier, writes JSON and Markdown results, and records timeout failures as explicit blocker evidence. Use it before claiming that a generated page, table, app module, sample-data import, Teams tab, Planner binding, or SharePoint/Outlook integration is live.

## Release Profiles

The ALM profiles catalog (`powerplatform/settings/alm-profiles.json`) defines four stages:

| Profile | Type | Live Writes | Purpose |
| --- | --- | --- | --- |
| `local` | Unmanaged | No | Repo validation, no environment required |
| `development` | Unmanaged | Yes | Dylan environment unmanaged development |
| `test` | Unmanaged | Gated | Shared test import (URL placeholder) |
| `enterprise` | Managed | Gated | Future managed deployment (URL placeholder) |

Select a profile with `-TargetProfileName` on import scripts:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-Solution.ps1 -TargetProfileName development
```

The `Resolve-AlmProfile.ps1` helper validates the profile exists and, for live-write profiles, resolves the target environment URL. Profiles that do not allow live writes (`allowLiveWrites: false`) will block solution import as a safety guard.

Release profiles documentation: [deployment-profiles.md](deployment-profiles.md)

## Non-Negotiable Gates

- No settings-file import until Dylan connection references and environment variables are populated.
- No AI Builder enablement without capacity and human review governance.
- No Graph/Azure/Entra dependency until access and admin approvals exist.
- No shared test deployment without connection ownership and rollback notes.
