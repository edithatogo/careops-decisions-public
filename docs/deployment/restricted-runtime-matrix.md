# Restricted Runtime Matrix

This matrix documents what works in the restricted-repo/local profile and what is blocked by missing live tenant dependencies.

Use it before each session to decide what to do in a constrained environment.

## Environment Modes

- `Local` — no live Power Platform writes; schema/code smoke checks and dry-runs.
- `Live + Connectors` — requires authenticated environment with Teams/Planner/SharePoint/Outlook/AI Builder and proper connection references.

Current live browser probe status for the active maker session:
- SharePoint and Outlook connections are visible and connected.
- Teams and Planner are not visible in the current Connections inventory.
- The `New connection` surface is available, but the default catalog view does not currently show Teams or Planner as immediate options.
- Teams is reachable in the browser session.
- Planner resolves at `https://planner.cloud.microsoft/`, but in this session it lands on the Planner sign-in landing page rather than an already authenticated board.

## Capability Matrix

| Capability | Local Mode | Local + Mocked/Seed | Live + Connectors | Why |
| --- | --- | --- | --- | --- |
| Model-driven app navigation and screen-level contracts | ✅ | ✅ | ✅ | Built in Dataverse source contracts and app-map contracts |
| Meeting + governance records (bodies, members, meetings, minutes, decisions, actions) | ✅ | ✅ | ✅ | Dataverse-first core |
| Feature switchboard logic | ✅ | ✅ | ✅ | Local feature metadata and overrides are contract-defined |
| Reporting views and governance dashboards (Dataverse views) | ✅ | ✅ | ✅ | Implemented in reporting contracts and smoke scenarios |
| Seed import planning and validation | ✅ | ✅ | ✅ | `Import-SeedData.ps1` supports dry-run/check mode |
| AI draft generation via AI Builder (`transcript-to-minutes`) | ⚪ (placeholder only) | ⚪ | ✅ | Requires AI Builder connector and capacity |
| AI review queue/provenance UI (`AIRunsScreen`) | ⚪ (placeholder screens) | ⚪ (placeholder screens) | ✅ | Needs live AI Builder run records and reviewed flow outputs |
| Teams post summary flows | ⚪ (disabled) | ⚪ (disabled) | ✅ | Requires Teams connection + valid channel target |
| Planner sync for actions | ⚪ (disabled) | ⚪ (disabled) | ✅ | Requires Planner connection + plan mapping in environment |
| SharePoint paper/document binding | ⚪ (document link placeholder) | ⚪ (document link placeholder) | ✅ | Requires SharePoint connector and site/library agreement |
| Outlook invite sync | ⚪ (not activated) | ⚪ (not activated) | ✅ | Requires Outlook connection + tenant mailbox/app permissions |
| Integration logs from live flow execution | ⚪ (no new events) | ⚪ (no new events) | ✅ | Requires executed connectors and flow runtimes |
| Team/role-based live connector readiness checks | ✅ (static checks) | ✅ (static checks) | ✅ | Readiness scripts validate local settings and connector declarations |

Legend: ✅ = executable, ⚪ = blocked/disabled in restricted version.

## What you can and cannot do in restricted mode

**Can**
- Run repository validation, structure checks, and release packaging with `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Invoke-RepoValidation.ps1`.
- Run smoke/runbook dry runs and seed-file checks.
- Maintain all Dataverse contracts, app contracts, flow contracts, and reporting definitions.
- Generate deployment evidence stubs, readiness output, and release artifacts without writing to an environment.

**Cannot**
- Validate any connector-triggering flow in a live sense.
- Verify live transcript-to-minutes, Teams posts, Planner/task sync, SharePoint storage flow, or Outlook invite sync.
- Validate whether Teams or Planner are exposed in the current maker connection catalog without first resolving tenant/admin scope.
- Treat browser reachability of Planner as separate from maker connection availability.
- Confirm integration log volume/quality from production connectors (only scaffold and contracts can be validated locally).

## Recommended execution by mode

### Local / restricted mode

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Invoke-RepoValidation.ps1 -RequireImplementationScaffolds
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Invoke-SmokeRunbook.ps1 -DryRun
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-DeploymentChecklist.ps1 -DryRun
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-ReleaseArtifact.ps1 -Unmanaged -DryRun
```

### Connector-enabled mode

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Get-EnvironmentReadiness.ps1 -TargetProfileName development -DeploymentSettingsPath powerplatform/settings/deployment-settings.local.json
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-Solution.ps1 -TargetProfileName development -PublishChanges
```

## Related docs

- `docs/deployment/deployment-profiles.md`
- `docs/guides/decisions-workspace-quick-start.md`
- `docs/testing/test-tagging-strategy.md`
- `tests/smoke/smoke-scenarios.md`
- `tests/powerapps/DecisionsWorkspace.testplan.yaml`
