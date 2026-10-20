# Implementation Roadmap

## Phase 0: Setup

- Conductor context and tracks.
- Tool and skill inventory.
- Repo-first Power Platform skeleton.
- Layout validation.

## Phase 1: Research And Architecture

- Decisions feature map.
- Template and kit evaluation.
- Dataverse and integration architecture.
- Microsoft platform integration map.

## Phase 2: MVP Activation And Enhancements

The v1.0.0 Dylan Dataverse source export is complete. This phase now focuses on turning that source baseline into an operational MVP.

- Dataverse-first app-core build before Teams/SharePoint deployment.
- App and flow activation from the exported source.
- Release artifacts, solution checker, and validation gates.
- Sample data import and reset/reseed flow.
- Configuration wizard for environment-specific setup.
- Human review gates for draft and AI-assisted content.
- Reporting surfaces and metric definitions.
- Test coverage for activation, import, review, and smoke scenarios.
- Local implementation hardening wave: seed payload validation, app contract validation, flow contract validation, reporting/data quality, operations readiness, and Conductor status.
- Per-meeting feature switchboard with meeting-series inheritance, meeting overrides, and environment-gated connector resolution.
- Pushed commits `8509078` and `185ac9c`; CI passed for `185ac9c`.
- DMS candidates limited to SharePoint document libraries, Teams file locations, document registration, and Word/PDF export paths.
- Local build work can finish before live connector/environment access exists; Teams, Planner, SharePoint, and Graph wiring waits on target environment permissions and references.

## Phase 3: Enterprise Upgrade

- Full Dataverse security and ALM.
- Entra group mapping.
- Graph transcript, calendar, Teams meeting, file, group, and user sync.
- Power BI/Fabric reporting.
- Teams meeting extension and Copilot integration.
- Design boundary and approval gates are captured in `docs/architecture/enterprise-upgrade-path.md`; implementation stays deferred until tenant access and approvals exist.

## Phase 4: Operating Model

- Governance reporting and Dataverse dashboards.
- Power BI/Fabric semantic model, row-level security, and metric definitions.
- Per-meeting feature switchboard reporting, audit traceability, and connector-availability reporting.
- DLP, retention, sensitivity, audit, and release-readiness gates.
- Support runbooks for connector failures, stale external IDs, missing files, stuck approvals, AI review exceptions, and access-rule errors.
- Add deployment runbooks for partially listed group/site/planner content, missing Teams and Planner connector references, and Graph group-specific 403 failures.
- Azure/Entra implementation only after tenant access, permissions, and governance approvals are available.

## Phase 5: Parallel Hardening Tracks

- Track 14: ALM, solution packaging, release pipeline, and rollback.
- Track 15: security, DLP, privacy, retention, external participants, and threat model.
- Track 16: governance reporting, Dataverse dashboards, Power BI/Fabric model, and metrics.
- Track 17: operations, monitoring, support runbooks, alerts, and continuity.
- Track 18: data migration, seed fixtures, import mappings, and environment bootstrap.
- Track 19: test automation, connector mocks, CI validation, and acceptance evidence.
- Track 20: ADRs, licensing/capacity, tenant permissions, service accounts, and readiness gates.
- Track 21: repo-side ALM activation, CI validation, and solution checker handoff.
- Track 22: MVP app/flow activation, release artifacts, solution checker, sample data import, configuration wizard, human review gates, reporting, tests, local implementation hardening, and the per-meeting feature switchboard across seed/app/flow/reporting/operations/status contracts.
- Track 23: Medication Safety Committee operationalization, user guide, Decisions feature coverage, AI Builder minutes pattern, NSW Outlook/SharePoint source gate, and ALM environment-variable closure.
- Track 24: agent workflow operations, writer/reviewer pairing, live deployment serialization, and Dylan verification.
