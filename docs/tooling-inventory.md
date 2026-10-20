# Tooling Inventory

## Local CLI And UI Tools

| Tool | Status | Primary Use |
| --- | --- | --- |
| `pac` | Available at `C:\Users\60217257\.dotnet\tools\pac.exe`; version `2.6.4.0` | Power Platform auth, environments, solution pack/unpack/import/export/check, canvas, test |
| `pacx` | Available at `C:\Users\60217257\.dotnet\tools\pacx.exe`; version `1.2026.3.195` | Dataverse table, column, relationship, workflow, solution, and script support |
| `XrmToolBox.cmd` | Available at `C:\Users\60217257\.local\bin\XrmToolBox.cmd` | Metadata browser, FetchXML Builder, SQL 4 CDS, Plugin Trace Viewer, admin tooling |
| `git` | Available at `C:\Users\60217257\scoop\apps\git\current\cmd\git.exe`; version `2.53.0.2` | Source control |
| `gh` | Available at `C:\Users\60217257\scoop\shims\gh.exe` | Remote repo, issues, pull requests, workflow checks |
| `node` | Available at `C:\Users\60217257\scoop\apps\nodejs-lts\current\node.exe`; version `24.15.0.0` | Helper scripts, validators, future Playwright/Test Engine support |
| `npm` | Available at `C:\Users\60217257\scoop\apps\nodejs-lts\current\npm.ps1` | Package scripts and future test dependencies |
| `.NET` | Available at `C:\Users\60217257\AppData\Local\Microsoft\dotnet\dotnet.exe`; version `11.0.26.20806` | Power Platform CLI runtime and future test utilities |
| `pwsh` | Available at `C:\Users\60217257\scoop\apps\pwsh-beta\current\pwsh.exe`; version `7.6.0.0` | Preferred PowerShell automation host |
| Windows PowerShell | Available at `C:\WINDOWS\System32\WindowsPowerShell\v1.0\powershell.exe` | Compatibility fallback |

## PowerShell Modules

No relevant Power Platform, Graph, Azure, or Teams PowerShell modules were found in the current module path during setup:

- `Microsoft.PowerApps.Administration.PowerShell`
- `Microsoft.PowerApps.PowerShell`
- `Microsoft.Xrm.Data.PowerShell`
- `Microsoft.Graph`
- `Az.Accounts`
- `Az.Resources`
- `MicrosoftTeams`

Prefer `pac` and `pacx` for the immediate MVP. Add these modules only when a live tenant and permission model justify them.

## ALM-Specific Command Reference

Key PAC and PACX commands used in this repository:

| Operation | PAC Command | PACX Equivalent | Wrapper Script |
| --- | --- | --- | --- |
| Solution pack | `pac solution pack` | — | `Pack-Solution.ps1`, `New-ReleaseArtifact.ps1` |
| Solution unpack | `pac solution unpack` | — | `Unpack-Solution.ps1` |
| Solution export | `pac solution export` | — | `Export-Solution.ps1` |
| Solution import | `pac solution import` | — | `Import-Solution.ps1` |
| Solution check | `pac solution check` | — | `Invoke-SolutionChecker.ps1` |
| Auth list | `pac auth list` | — | — |
| Auth select | `pac auth select` | — | `Resolve-AlmProfile.ps1` |
| Table list/create | — | `pacx table list/create` | — |
| Column add | — | `pacx column add` | — |
| Choice manage | — | `pacx choice list/add` | — |
| Relationship create | — | `pacx relationship create` | — |

Full command reference: [docs/deployment/pac-cli-reference.md](deployment/pac-cli-reference.md)

## Skills And Agents

| Capability | Use |
| --- | --- |
| `conductor-setup` | Initial product, stack, workflow setup |
| `conductor-newtrack` | Track creation |
| `conductor-implement` | Track execution |
| `conductor-status` | Evidence-based status |
| `conductor-test` | Validation gate execution |
| `conductor-review` | Review and risk pass |
| `dataverse` | Dataverse model, metadata, querying, and ALM support |
| `Power BI Deployment` | Future Power BI/Fabric workspace, dataset, and report deployment planning |
| `Power BI Modeling` | Governance reporting semantic model design |
| `Power BI Security` | Row-level security, workspace permissions, and sensitivity handling |
| `Power BI Documentation` | Report and semantic-model documentation |
| `Power BI Report` | Report page and measure implementation once reporting data is available |
| `Power Platform MCP Connectors` plugin | Future custom connector and Copilot Studio/MCP integration design |
| `Power Apps Code Apps` plugin | Future code-first app surface or advanced Power Apps implementation guidance |
| `azure-prepare` | Azure readiness and prerequisite inventory before tenant access exists |
| `azure-rbac` | Future Azure role and permission mapping |
| `azure-deploy` | Future Azure deployment when access is granted |
| `azure-validate` | Future Azure validation gate |
| `azure-compliance` | Data handling, audit, and compliance review for Azure-hosted services |
| `entra-app-registration` | Future app registration, Graph permission, and service principal design |
| Worker subagents | Parallel track implementation with disjoint write sets |
| Explorer subagents | Focused repo/tool research |

## Available Agent Tracks

The current conductor plan can be parallelized into these workstreams:

- Worker A: solution ALM, `pac`/`pacx` command binding, solution checker, release packaging.
- Worker B: security roles, access model, DLP assumptions, audit model, records management.
- Worker C: model-driven app, Teams/canvas app surface, app accessibility and Test Engine coverage.
- Worker D: Outlook/Teams invite sync, Planner sync, SharePoint document registration, connector smoke tests.
- Worker E: Azure/Entra design pack, Graph permissions, service principals, managed identity path, implementation gates.
- Worker F: governance reporting, Power BI/Fabric semantic model, Dataverse views, operational dashboards.
- Worker G: operations, monitoring, support runbooks, alerts, continuity.
- Worker H: data bootstrap, import mappings, fixture packs, migration policy.
- Worker I: test automation, connector mocks, CI wrapper, acceptance evidence.
- Worker J: ADRs, licensing/capacity, tenant permissions, service account and readiness gates.
- Explorer agents: tool capability checks, source-map updates, external template and kit evaluation, validation gap analysis.
- Track 22 app-core work is split across six `gpt-5.4-mini` medium workers: activation baseline, release artifacts and solution checker, sample data and configuration wizard, human review gates, reporting, and tests/validation.

## Connector And Platform Assumptions

- Office 365 Users for manager/direct-report lookup in the MVP.
- Outlook for meeting invite creation/update where user permissions allow.
- Teams for channel notifications and app hosting.
- Planner for action item follow-through.
- SharePoint/Teams files for agenda, paper, and minute storage where available.
- AI Builder prompts for transcript-to-minutes once licensing/capacity is confirmed.
- Microsoft Graph for later transcript automation and richer directory/calendar sync.
- DMS candidates are SharePoint document libraries, Teams file locations, document registration flows, and Word/PDF export paths.
- Current live blocker is that Teams and Planner connector references are missing in the target Power Platform environment, while some group-specific Graph calls return 403 even though group/site/planner content can be partially listed.

## Governance And Reporting Tooling

- Dataverse views and dashboards should provide first-line operational reporting before Power BI exists.
- Power BI/Fabric should become the enterprise reporting layer once a stable Dataverse environment exists.
- CoE Starter Kit should be referenced for Power Platform governance and operational reporting patterns, but not embedded as a dependency.
- Reporting outputs should cover meeting cadence, attendance, quorum, agenda throughput, paper timeliness, decisions, actions, overdue actions, approval latency, conflict declarations, access-rule changes, AI prompt runs, integration failures, and records-retention status.
