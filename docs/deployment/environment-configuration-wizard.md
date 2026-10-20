# Environment Configuration Wizard

This repo-local checklist is generated from the settings manifests and the sample deployment settings file.

Use it to confirm the values that must be present before packaging, import, or smoke testing.

Use the generator to refresh this file from the repo state:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-EnvironmentConfigurationChecklist.ps1
```

## Configuration Checklist

- [ ] Environment variables are populated in `powerplatform/settings/deployment-settings.sample.json`.
- [ ] Connection references are populated in `powerplatform/settings/deployment-settings.sample.json`.
- [ ] Teams team and channel IDs point to the intended workspace.
- [ ] Planner plan ID points to the intended action plan, if Planner sync is enabled.
- [ ] SharePoint site URL points to the document library host for agendas, papers, minutes, transcripts, and packs.
- [ ] AI Builder minutes toggle stays disabled until licensing, capacity, and review governance are confirmed.
- [ ] Placeholder values are removed before any live import.

## Environment Variables

| Schema name | Purpose | Required for MVP | Sample value | Status |
| --- | --- | --- | --- | --- |
| `decisions_TeamsTeamId` | Teams team used for app and channel context. | Yes | `00000000-0000-0000-0000-000000000000` | Needs review |
| `decisions_TeamsChannelId` | Teams channel for governance notifications. | Yes | `00000000-0000-0000-0000-000000000000` | Needs review |
| `decisions_PlannerPlanId` | Planner plan for action sync. | No | `planner-plan-id` | Needs review |
| `decisions_SharePointSiteUrl` | SharePoint site for agendas, papers, minutes, transcripts, and packs. | Yes | `https://contoso.sharepoint.com/sites/decisions-workspace` | Needs review |
| `decisions_MinutesLibraryName` | Document library used for meeting records. | Yes | `Shared Documents` | Ready |
| `decisions_EnableAiBuilderMinutes` | Feature flag for AI Builder transcript-to-minutes. | No | `false` | Ready |
| `decisions_EnableGraphTranscriptSync` | Future Graph transcript automation gate. | No | `false` | Ready |
| `decisions_AiBuilderPromptName` | AI Builder prompt name used for transcript-to-minutes drafting when enabled. | No | `Decisions Workspace - Transcript to Minutes` | Ready |
| `decisions_IntegrationLogRetentionDays` | Default retention for integration logs. | Yes | `90` | Ready |

## Connection References

| Logical name | Connector | Purpose | Required for MVP | Owner strategy | Status |
| --- | --- | --- | --- | --- | --- |
| `decisions_sharedcommondataserviceforapps` | `/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps` | Dataverse records and AI Builder prompt run metadata. | Yes | Use named maker connection only in local/dev; use governed connection owner for shared environments. | Needs review |
| `decisions_sharedteams` | `/providers/Microsoft.PowerApps/apis/shared_teams` | Teams channel notifications and Teams app context. | Yes | Connection owner should also be Teams owner or approved app installer. | Needs review |
| `decisions_sharedplanner` | `/providers/Microsoft.PowerApps/apis/shared_planner` | Action item task sync. | No | Connection owner should own or co-own the target Planner plan. | Needs review |
| `decisions_sharedsharepointonline` | `/providers/Microsoft.PowerApps/apis/shared_sharepointonline` | Agenda, paper, minutes, transcript, and pack storage. | Yes | Connection owner should have library owner or elevated contributor rights. | Needs review |
| `decisions_sharedoffice365users` | `/providers/Microsoft.PowerApps/apis/shared_office365users` | Person profile, manager, and reporting hierarchy cache. | Yes | Connection owner must be allowed to resolve users and manager relationships. | Needs review |
| `decisions_sharedoffice365` | `/providers/Microsoft.PowerApps/apis/shared_office365` | Outlook meeting invite sync. | Yes | Use a mailbox or service identity acceptable for meeting creation. | Needs review |
| `decisions_sharedaibuilder` | `/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps` | AI Builder transcript-to-minutes prompt execution when enabled. | No | Enable only after AI Builder capacity and human review governance are confirmed. | Needs review |

## Targeted Readiness Checks

### Teams

- [ ] `decisions_TeamsTeamId` is set to the intended team.
- [ ] `decisions_TeamsChannelId` is set to the intended channel.
- [ ] The Teams connection reference resolves to the correct maker or service connection.

### Planner

- [ ] `decisions_PlannerPlanId` points to the intended plan when Planner sync is in scope.
- [ ] The Planner connection reference resolves and has access to the target plan.
- [ ] Planner can remain unset when the deployment does not use task sync.

### SharePoint

- [ ] `decisions_SharePointSiteUrl` points to the correct site.
- [ ] `decisions_MinutesLibraryName` points to the intended document library.
- [ ] The SharePoint connection reference resolves to an account with the right library access.

### AI Builder

- [ ] `decisions_EnableAiBuilderMinutes` stays `false` unless licensing and capacity are confirmed.
- [ ] `decisions_AiBuilderPromptName` matches the prompt published in the target environment.
- [ ] The Dataverse connection reference used for AI Builder has the required rights.
- [ ] Human review remains part of the transcript-to-minutes path.

## Suggested Safe Order

1. Confirm the deployment settings file is present.
2. Populate environment variables and replace placeholder values.
3. Map connection references to the intended maker or service connections.
4. Confirm Teams, Planner, and SharePoint targets.
5. Leave AI Builder disabled until capacity and governance are signed off.
6. Re-run the readiness scripts before any import.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Get-EnvironmentReadiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-EnvironmentConfigurationChecklist.ps1
```
