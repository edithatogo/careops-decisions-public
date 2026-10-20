# Deployment Checklist

Generated checklist for the current Decisions Workspace scaffold.

Use the checklist generator to refresh this file from the live repo state:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-DeploymentChecklist.ps1
```

## Checklist

- [ ] `pac` CLI is available
  - Install Power Platform CLI or fix `PATH` resolution if missing.
- [ ] `pac auth list` shows an active environment
  - Select the target environment before packing or importing.
- [ ] `pacx` is available
  - Continue with `pac` if `pacx` is not installed.
- [ ] Solution folder exists
  - `powerplatform/solutions/DecisionsWorkspace`
- [ ] `Solution.xml` matches the expected solution name
  - `DecisionsWorkspace`
- [ ] Required environment variables exist in deployment settings
  - `decisions_TeamsTeamId`
  - `decisions_TeamsChannelId`
  - `decisions_PlannerPlanId`
  - `decisions_SharePointSiteUrl`
  - `decisions_EnableAiBuilderMinutes`
  - `decisions_AiBuilderPromptName`
  - `decisions_IntegrationLogRetentionDays`
- [ ] Placeholder environment variable values are replaced
  - Replace scaffold values before a live import.
- [ ] Required connection references exist in deployment settings
  - `decisions_sharedcommondataserviceforapps`
  - `decisions_sharedteams`
  - `decisions_sharedplanner`
  - `decisions_sharedsharepointonline`
  - `decisions_sharedoffice365users`
  - `decisions_sharedoffice365`
  - `decisions_sharedaibuilder`
- [ ] Placeholder connection IDs are replaced
  - Replace `replace-with-...` values before import.
- [ ] Dylan local settings are generated only into ignored files
  - Use `powerplatform/settings/deployment-settings.local.json`; do not commit live connection IDs, resource IDs, tokens, or secrets.
- [ ] Missing Dylan connector states are resolved or explicitly deferred
  - Current live activation blockers include Teams, Planner, and Office 365 Users until connected references bind in the local settings generator.
- [ ] Graph 403 responses are documented
  - Capture endpoint, object ID, account, and required permission; treat as a live activation blocker rather than an app-core blocker.
- [ ] Failed import retry criteria are met
  - Retry only after missing dependencies, placeholder settings, inactive connections, or target-profile issues have been corrected.
- [ ] Seed payloads are current
  - Re-run `scripts\Import-SeedData.ps1 -DryRun` after seed JSON changes and do not reuse stale `artifacts/seed/` payloads.
- [ ] AI human review gate is active before AI output can publish
  - Leave AI Builder disabled unless capacity, licensing, prompt ownership, and reviewer evidence are confirmed.

## Safe Command Order

1. Review the readiness report.
2. Generate or refresh ignored Dylan local settings if targeting the development environment.
3. Confirm readiness has no missing required connection references or placeholder settings for the settings file being imported.
4. Refresh seed dry-run artifacts if smoke data changed.
5. Pack the unmanaged solution zip.
6. Run solution check on the zip.
7. Import only when the target environment is confirmed and failed-import retry criteria are clear.
8. Capture read-only Dylan evidence after import or generated-page changes.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Get-EnvironmentReadiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-LocalDeploymentSettings.ps1 -DryRun
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Get-EnvironmentReadiness.ps1 -TargetProfileName development -DeploymentSettingsPath powerplatform/settings/deployment-settings.local.json
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-SeedData.ps1 -DryRun
pac solution pack --folder powerplatform/solutions/DecisionsWorkspace --zipfile artifacts/solutions/DecisionsWorkspace_unmanaged.zip --packagetype Unmanaged
pac solution check --path artifacts/solutions/DecisionsWorkspace_unmanaged.zip
pac solution import --path artifacts/solutions/DecisionsWorkspace_unmanaged.zip --settings-file powerplatform/settings/deployment-settings.local.json --publish-changes
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-DylanDeploymentEvidence.ps1 -PacTimeoutSeconds 30
```

Use the committed sample settings only for source validation and examples. Use the ignored local settings file for Dylan live activation.
