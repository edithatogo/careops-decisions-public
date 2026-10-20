# Operations Runbooks

Use these runbooks for local app-core support before any live Teams, Planner, SharePoint, Graph, or AI Builder activation. The local app-core is Dataverse-first: connector failures should be isolated, logged, and either skipped or kept behind feature flags instead of blocking record-level smoke testing.

## First Triage

1. Confirm the environment and settings file being tested.
   - Local/source checks usually use `powerplatform/settings/deployment-settings.sample.json`.
   - Dylan development checks use ignored local settings: `powerplatform/settings/deployment-settings.local.json`.
2. Run readiness in read-only mode.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Get-EnvironmentReadiness.ps1 -TargetProfileName development -DeploymentSettingsPath powerplatform/settings/deployment-settings.local.json -Json
```

3. Classify the issue as source, settings, connection, permission, import, seed, or human-review governance.
4. Record the correlation ID, Power Automate run ID, app session ID, source record natural key, and settings file path used for the test.
5. Do not retry connector-backed scenarios repeatedly when the readiness output already shows a missing reference, placeholder value, or known permission blocker.

## Support Matrix

| Symptom | Likely cause | Triage action | Escalate when |
| --- | --- | --- | --- |
| Connector reference is missing for Teams, Planner, Office 365 Users, SharePoint, Outlook, Dataverse, or AI Builder | The connection does not exist in the maker environment or the local settings generator could not bind it | Run `scripts\New-LocalDeploymentSettings.ps1 -DryRun`, inspect `MissingConnectionReferences`, create or repair the connection in Dylan, then regenerate `deployment-settings.local.json` | A required connector is missing after the maker confirms it exists and is connected |
| Graph or group-specific call returns 403 | The user or app lacks Graph, group, site, channel, or Planner permission; Graph automation is not approved yet | Treat the call as a live activation blocker only. Keep local Dataverse smoke scenarios running and log the failed endpoint, object ID, and account | Admin consent, group ownership, or app registration approval is required |
| Placeholder setting remains in deployment settings | Sample values such as `replace-with-...`, empty values, or scaffold IDs were used for import | Stop import. Replace only in ignored local settings, not in committed sample files. Re-run readiness and confirm placeholder arrays are empty | A live import was attempted with placeholder values |
| Solution import fails | Missing dependency, invalid settings file, inactive connection reference, target profile mismatch, or unmanaged layer conflict | Capture the import log, exact command, solution zip path, settings file path, and target profile. Re-run readiness before another import | The failure persists after all missing references/placeholders are resolved |
| Seed payload looks stale or smoke data does not match expected records | `powerplatform/src/seed/sample-governance-data.json` changed after artifacts were generated, or an old import plan was reused | Re-run `scripts\Import-SeedData.ps1 -DryRun` and use the regenerated plan under `artifacts/seed/`. Confirm natural keys before any Dataverse upsert implementation | Seed counts or relationships differ from `tests/smoke/seed-data-checklist.md` |
| AI minutes or summary publishing is blocked | AI Builder is disabled, capacity is unavailable, or human review evidence is missing | Keep `decisions_EnableAiBuilderMinutes` disabled unless capacity and review governance are signed off. Use manual transcript-to-minutes smoke testing | AI output can be published without reviewer, review time, and decision evidence |
| Failed Power Automate flow | Settings, connector, permissions, malformed payload, or missing source record | Open the run history, copy the failed action, run ID, source record, and environment variable values used by the flow. Check integration logs before retry | The failure affects a data write, notification, approval, or external document action |
| Stale Outlook or Teams meeting sync | Connector is skipped/missing, Graph access is denied, or the local app-core record has no current external binding | Check the binding record and integration log. If the connector is not enabled, mark the smoke scenario skipped with reason | The connector is enabled and the external record exists but sync remains stale |
| Unresolved attendee or missing `OrgPerson` | Directory lookup failed, Office 365 Users connection is missing, or seed/manual person data is incomplete | Add or correct the Dataverse person record for local testing. Do not require live directory lookup for app-core smoke | Live directory resolution is required for deployment acceptance |
| Missing paper, agenda, transcript, or minutes file | SharePoint location is missing, document library is not bound, or document registration did not complete | Confirm the Dataverse document record first, then the SharePoint site/library only when the SharePoint connector is enabled | A document is referenced in an approved meeting pack but cannot be opened |
| Stuck minutes approval | Approval state, reviewer assignment, or human review evidence is incomplete | Confirm status, reviewer, review timestamp, and audit trail on the minutes record | Approval cannot be completed by the assigned secretary/chair role |
| Orphaned Planner task | Planner connector is missing or the Dataverse action no longer maps to a task ID | Keep the Dataverse action as source of truth. Re-link only after Planner is enabled and the plan ID is confirmed | A live deployment requires Planner task recovery |
| Access-rule or classification error | Role, committee membership, classification, or sharing rule is wrong | Verify the signed-in role, committee membership, record classification, and expected access rule | A user can see restricted records or cannot see records required for their role |

## Related Documents

- `docs/operations/agent-runbook.md` — agent lane ownership, writer/reviewer pairing, parallel work rules, and live Dylan deployment serialization.
- `docs/operations/medication-safety-committee-workflows.md` — Medication Safety Committee setup, pre-meeting/meeting/post-meeting workflow, AI Builder usage, sample records, and reporting.
- `docs/deployment/deployment-checklist.md` — deployment prerequisites, safe command order, environment variable and connection reference validation, and AI review gate checks.
- `docs/testing/smoke-runbook.md` — local smoke test runbook for manual or semi-automated verification.
- `scripts/Verify-DylanDeployment.ps1` — read-only Dylan verification for app module, generated pages, and environment state.
- `scripts/New-DylanDeploymentEvidence.ps1` — post-deployment evidence capture script.

## Business Continuity

For a connector outage, export or capture the Dataverse agenda, minutes, decisions, and actions first. Connector-backed Teams posts, Planner tasks, SharePoint files, Outlook sends, and AI summaries are secondary artifacts until live activation is complete.

## Agent Workflow

Use `docs/operations/agent-runbook.md` when splitting work across subagents. It defines lane ownership, writer/reviewer pairing, live Dylan serialization rules, and the read-only deployment verification path.
