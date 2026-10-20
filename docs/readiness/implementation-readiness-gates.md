# Implementation Readiness Gates

Each gate defines what passes, what fails, and who decides. A gate must be
signed off before proceeding to the next environment.

---

## Local Gate

Scope: Local development and source-driven validation.

| Check | Pass | Fail | Decider |
| ----- | ---- | ---- | ------- |
| Repo validation passes | `Invoke-RepoValidation.ps1` exits with code 0 | Any validation error | CI / developer |
| Generated artifacts are current | All committed artifacts match generator output | Artifact drift detected | CI / reviewer |
| Track specs and plans exist | All 24 tracks have spec.md, plan.md, metadata.json | Any track missing definition | Conductor owner |
| No live tenant dependencies | All seed and smoke data uses local-only references | Any hard-coded tenant ID or connection | CI / developer |
| Schema build plan is current | `schema-build-plan.json` matches `dataverse-tables.yaml` | Schema drift detected | CI / reviewer |
| Flow contracts reference only available connectors | Flow catalog uses only Dataverse, Outlook, Teams, Planner, SharePoint, Office 365 Users, AI Builder | Flow references Graph, Azure, or custom connector | CI / reviewer |
| ADR consistency | All ADR statuses are current and no ADR is in "proposed" without rationale | ADR in draft state | Reviewer |

---

## Power Platform Development Gate

Scope: Active development in a Power Platform development environment with
maker and customizer access.

| Check | Pass | Fail | Decider |
| ----- | ---- | ---- | ------- |
| Target environment and publisher confirmed | Environment URL and publisher prefix match deployment settings | Mismatch or placeholder | Developer |
| Maker and Dataverse customization permissions confirmed | Maker can create tables, flows, and apps | Permission denied on create | Developer / Power Platform admin |
| Connection owners identified | Each required flow connector has a named connection owner | Missing owner for any connector | Developer |
| Deployment settings populated | `deployment-settings.local.json` has no placeholder values | Any `replace-with-...` value present | Developer / CI |
| Unmanaged solution export/import path confirmed | Developer has imported and exported the solution at least once | Never attempted | Developer |
| Environment variables seeded | All required environment variables exist with sample values | Missing or empty environment variable | Developer |
| Sample data imported | `Import-SeedData.ps1` runs without error and seed records are queryable | Import fails or no records created | Developer |

---

## Shared Test Gate

Scope: Pre-production validation in a shared test environment before user
acceptance testing.

| Check | Pass | Fail | Decider |
| ----- | ---- | ---- | ------- |
| Security roles implemented | All roles from security-role-matrix.yaml are created and assigned to test accounts | Any role missing | Governance Administrator |
| DLP assumptions reviewed | DLP policy document has been reviewed by Power Platform admin | DLP not reviewed | Power Platform admin |
| Seed and smoke data available | Sample governance data imported and all smoke scenarios pass | Any smoke scenario fails | Developer / tester |
| Flow owners and run-only users identified | Every flow has an owner and a run-only user (where applicable) | Missing owner | Developer |
| Release and rollback notes prepared | Release notes document the deployment order and rollback steps | No release or rollback plan | Developer / reviewer |
| Access control tested | Secretary, Chair, Member, Auditor, Restricted Reader roles tested with separate accounts | Any role can see records it should not | Tester |
| External participant access tested | Guest Presenter role tested with a B2B guest account | Guest can see records outside scope | Tester |
| AI human review gate verified | AI Builder run requires human review before minutes can be published | AI output published without review | Tester |

---

## Future Azure / Entra Gate

Scope: Enterprise path with Azure integration, Graph access, and Entra
group-based security.

| Check | Pass | Fail | Decider |
| ----- | ---- | ---- | ------- |
| Tenant admin approvals confirmed | Tenant admin has approved app registration and Graph permissions | No approval or pending | Power Platform admin |
| App registration and Graph permissions approved | App registration exists with correct Graph permissions | Registration missing or wrong scopes | Entra admin |
| Azure RBAC assigned | Required Azure RBAC roles are assigned for integration services | Roles missing | Azure admin |
| Managed identity or service principal model confirmed | Service principal or managed identity exists for background flows | No service principal | Entra admin |
| Monitoring, DLP, retention, and reporting implications reviewed | Impact of Azure/Entra on monitoring, DLP, retention, and reporting is documented | No review conducted | Governance Administrator |
| Entra group-to-Dataverse team mapping defined | Mapping from Entra security groups to Dataverse teams is documented | No mapping defined | Governance Administrator |
| Service principals have Dataverse roles | Each service principal has the correct Service Account role | Missing role assignment | Dataverse admin |

The design boundary for this gate is `docs/architecture/enterprise-upgrade-path.md`. Passing this gate authorizes implementation planning; it does not by itself authorize live Azure, Entra, Graph, Teams extension, Copilot, or Power BI/Fabric deployment.
