# Implementation Status

Status as of 2026-05-04.

## Done

- Conductor setup and track map.
- Public Decisions feature research.
- Template and kit evaluation.
- Power Platform architecture and data model documentation.
- Microsoft 365 integration map.
- ALM folder skeleton and validation script.
- Power Platform source placeholders for data model, flow contracts, solution metadata, deployment settings, and seed data.
- Smoke-test strategy and scenario list.
- Dataverse schema YAML scaffold.
- Teams canvas app screen map and Power Fx patterns.
- Canvas app MVP source contracts for navigation, screens, state transitions, commands, and flow cross-references.
- Power Automate flow scaffolds.
- Automation integration source contracts for Planner, Teams summary posts, reminders, SharePoint file registration, and app command cross-references.
- Future Azure/Entra/Graph upgrade boundary documented without adding MVP runtime dependencies.
- Governance model seed now covers parent bodies, committees, a nested working group, ToR version history, quorum variants, and varied membership roles.
- Meeting-record seed now covers agenda items, papers, draft/approved/published minutes, decisions, and actions linked back to the same meeting workflow.
- Invite and attendance seed now covers accepted attendees, apologies, proxy attendance, a non-voting guest, Outlook attendee identifiers, and quorum-ready voting evidence.
- Transcript and AI review seed now covers a manual transcript, approved prompt, completed AI Builder run, draft structured output, and pending human review queue evidence.
- Power Apps Test Engine skeleton.
- Source artifact and JSON/YAML validators.
- Environment readiness read-only checker.
- Deployment checklist generator.
- Implementation readiness validator.
- Dataverse schema build runner.
- Flow implementation checklist generator.
- App implementation checklist generator.
- Phase 3 hardening tracks 14-20.
- ADRs for major architecture choices.
- Licensing, tenant permissions, service account, and readiness gate docs.
- Skill, agent, and tool matrix.
- Operations runbook index.
- Track 21 repo-side ALM activation artifacts.
- Track 14 ALM solution release path: unmanaged pack/unpack round-trip, dry-run import, solution checker docs, release notes, and rollback strategy.
- Track 15 security, DLP, privacy, retention, external participant, and threat-model controls with schema coverage validation.
- Track 16 governance reporting: Dataverse dashboard contract, metric dictionary, reporting smoke scenarios, and deferred Power BI/Fabric boundary.
- Track 17 operations monitoring and support: ten support runbooks, explicit operational alert fields, business-continuity export paths, support roles/escalation, and operations contract validation.
- Track 18 data migration and bootstrap: fixture pack contracts, import mapping templates tied to schema names, bootstrap runbook, schema migration policy, and data bootstrap contract validation.
- Track 19 test automation and CI: CI validation parity, Power Apps suite readiness tags, connector skip strategy, acceptance evidence metadata, and test automation contract validation.
- Track 20 readiness gates: ADR coverage, licensing/capacity assumptions, tenant permission gates, service-account ownership strategy, environment pass/fail gates, and readiness contract validation.
- Track 22 MVP activation: release and solution checker path, sample data import/reset, configuration wizard, feature switchboard, human review gates, reporting, DMS candidates, tests, and live connector blocker separation with MVP activation contract validation.
- Track 23 Medication Safety operationalization: Teams guide, role guide, feature coverage audit, Medication Safety operating model, AI Builder minutes pattern, synthetic sample data, NSW Outlook/SharePoint source gate, and Medication Safety contract validation.
- Track 24 agent workflow operations: lane ownership, disjoint write sets, writer/reviewer pairings, serialized live Dylan writes, read-only Dylan verification, handoff evidence, and agent workflow contract validation.
- v1.0.0 Dylan Dataverse source export complete.
- Track 22 MVP activation and enhancement scaffold.
- Per-meeting feature switchboard design note and status framing.
- GitHub Actions validation workflow.
- Solution checker wrapper and ALM activation validator.
- Restricted runtime matrix and versioning boundary docs.
- Dylan unmanaged solution import and publish completed for `decisionsworkspace` / `CareOps Decisions` 1.0.0.0.
- Dylan local deployment settings generator and placeholder-safe import guard.
- Local Dataverse-first app-core build framing for track 22.
- Commits `8509078` and `185ac9c` pushed.
- CI passed for `185ac9c`.

## In Progress

- MVP app and flow activation, connector/environment-variable binding, sample data live import, human review gates, reporting, and tests.
- Local build is feature-complete for app-core, release artifacts, validation, and reporting scaffolds; live Teams/Planner/SharePoint deployment still waits on environment connector references and permissions.
- Current six-worker implementation wave is local hardening: seed payload validation, app contract validation, flow contract validation, reporting/data quality, operations readiness, and Conductor status.
- Reporting/data quality now includes the governance hierarchy, feature-switch adoption, connector health, and review-gate coverage the MVP dashboard contract needs.
- Local build completeness is tracked against repo-side source contracts and validation; live Teams, SharePoint, Planner, and Graph deployment blockers are tracked separately as environment/permission work.
- DMS candidates to carry forward are SharePoint document libraries, Teams file locations, document registration, and Word/PDF export.
- Current blocker: the browser session currently shows SharePoint and Outlook connections as available, Teams is reachable in-browser, and Planner resolves to the browser landing page, but Teams and Planner do not appear in the maker connection inventory; live Graph group-specific calls can still return 403 until the missing connector/admin scope is resolved.
- The per-meeting feature switchboard is now documented as a meeting-series-to-meeting inheritance layer with governance, reporting, audit, and environment-gated connector implications; implementation stays aligned to Track 22.

## Validation Commands

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-AlmLayout.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-SourceArtifacts.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-SourceArtifacts.ps1 -RequireImplementationScaffolds
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-JsonYaml.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Invoke-RepoValidation.ps1 -RequireImplementationScaffolds
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Get-EnvironmentReadiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-ImplementationReadiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-ImplementationReadiness.ps1 -Strict
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-PlanningArtifacts.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-AlmActivation.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Get-EnvironmentReadiness.ps1 -TargetProfileName development
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-LocalDeploymentSettings.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Get-EnvironmentReadiness.ps1 -TargetProfileName development -DeploymentSettingsPath powerplatform/settings/deployment-settings.local.json
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-Solution.ps1 -TargetProfileName development -PublishChanges
```

## Next Build Gate

Move from imported solution baseline to configured app-core, then into live deployment:

1. Finish the local Dataverse-first app-core path: activation baseline, release artifacts, sample data, configuration, human review gates, reporting, and tests.
2. Complete the current local hardening wave for seed payloads, app contracts, flow contracts, reporting/data quality, operations readiness, and Conductor status.
3. Create the missing Dylan connector connections for Teams, Planner, SharePoint, and Office 365 Users in the target environment.
4. Resolve the group/site/planner identity and permission gaps that are currently showing partial listings and 403s on some Graph group-specific calls.
5. Run the configuration wizard with real Dylan connector targets.
6. Convert the deterministic seed import plan into governed Dataverse upserts.
7. Activate the app and flows incrementally.
8. Verify human review gates and reporting surfaces.
9. Run smoke tests using the sample seed data.
10. Complete phase 3 hardening tracks 14-22 before shared test or production use.
11. Keep the per-meeting feature switchboard documented and aligned with the Track 22 activation path and environment-gated connectors.
12. Keep Medication Safety real-data import gated to NSW Outlook, SharePoint, Teams, or user-provided exports; synthetic sample validation is complete, but live committee records are not imported.
13. Use the agent runbook for future parallel work: keep source-agent write sets disjoint and route live Dylan writes through one deployment operator with read-only verification afterward.
