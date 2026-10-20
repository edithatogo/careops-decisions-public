# Environment Readiness

Use this before any live Power Platform import or schema creation.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Get-EnvironmentReadiness.ps1
```

The readiness check is read-only. It reports:

- `pac`, `pacx`, Git, Node/npm, and .NET availability.
- `pac auth list` output.
- Deployment settings sample presence.
- Environment variable and connection reference names.
- Suggested next commands.

Do not import the solution until the target environment is confirmed and the placeholder deployment settings have been replaced with real connection IDs and environment values.

The current readiness baseline is read-only and can partially list group, site, and Planner content. That is not deployment proof: Teams and Planner connector references are still missing in the Power Platform target, and some Graph group-specific calls return 403. Treat that as a blocker for live activation, not a blocker for the local Dataverse-first app-core build.

Document-management candidates to verify here are SharePoint document libraries, Teams file locations, document registration, and any Word/PDF export path used for packs or minutes.

## Dylan Development Expectations

Use the Dylan development environment only with ignored local settings. Do not hardcode secrets, tenant-specific connection IDs, or personal tokens into committed files.

Expected local workflow:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-LocalDeploymentSettings.ps1 -DryRun
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-LocalDeploymentSettings.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Get-EnvironmentReadiness.ps1 -TargetProfileName development -DeploymentSettingsPath powerplatform/settings/deployment-settings.local.json -Json
```

Current Dylan support posture:

- Dataverse, SharePoint, Office 365 Outlook, and the AI Builder Dataverse reference may be bindable from the current connection inventory.
- Microsoft Teams, Planner, and Office 365 Users must be treated as missing until the maker environment shows connected references and the local settings generator binds them.
- `decisions_TeamsTeamId`, `decisions_TeamsChannelId`, `decisions_PlannerPlanId`, and `decisions_SharePointSiteUrl` must be supplied from the real target resources before settings import.
- Graph 403 responses are live activation blockers. They do not invalidate the local app-core smoke path unless the scenario being tested requires that live Graph call.
- AI Builder must remain off unless capacity, licensing, prompt ownership, and the human review gate are all confirmed.

## Readiness Gates

| Gate | Required evidence before live import |
| --- | --- |
| Target environment | `pac auth list` active environment matches the selected ALM profile and `TargetEnvironmentUrl` is concrete |
| Deployment settings | No placeholder environment variables or connection reference IDs in the settings file used for import |
| Connector references | Dataverse, Teams, Planner, SharePoint, Office 365 Users, Office 365 Outlook, and AI Builder references are present; optional connectors can stay disabled but must not be ambiguous |
| Permissions | Any Graph, Teams, Planner, or SharePoint call required by activation has been tested with the same account/connection owner |
| Import | Solution pack and solution check complete before import; failed imports have captured logs and are not retried with the same unresolved blockers |
| Seed data | Seed dry-run has been refreshed after any seed JSON change and stale payload artifacts are not reused |
| AI review | AI output cannot publish unless reviewer, review time, prompt/run evidence, and accept/edit/reject state are recorded |

## Known Blocker Handling

- Connector missing: repair or create the connection in Dylan, regenerate `deployment-settings.local.json`, then re-run readiness.
- 403 Graph call: capture endpoint, object ID, account, and required permission. Escalate for tenant/admin approval rather than changing app-core behavior.
- Placeholder settings: stop before import. Replace values only in ignored local settings or the target environment.
- Failed import: attach import log, solution zip path, settings file path, target profile, and readiness JSON to the support note.
- Stale seed payload: re-run `scripts\Import-SeedData.ps1 -DryRun` and compare counts with `tests/smoke/seed-data-checklist.md`.
- AI human review blocker: keep the feature disabled or keep the record in draft until the review gate evidence exists.
