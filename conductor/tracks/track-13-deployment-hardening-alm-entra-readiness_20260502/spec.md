# Track 13: Deployment Hardening, ALM, And Entra Readiness

## Goal

Turn the current implementation-ready scaffold into a deployable Power Platform MVP while keeping Azure and Entra work designed but gated until access is available.

## Recommendation

Use ALM from this point forward. The app is broad enough that ad hoc maker edits will become hard to reconcile quickly: Dataverse schema, model-driven or canvas surfaces, Power Automate flows, environment variables, connection references, security roles, test plans, and future Azure/Entra components all need source control and repeatable promotion.

## Scope

- Convert the placeholder solution into a real unmanaged Power Platform solution source.
- Bind generated Dataverse schema steps to confirmed `pac` or `pacx` commands.
- Implement security roles and access-control conventions.
- Build a minimal model-driven administration surface and a lightweight Teams/canvas meeting workflow surface.
- Implement one real integration flow end to end before broadening the flow set.
- Add seed data and repeatable smoke-test fixtures.
- Strengthen validation beyond file presence into dependency and coverage checks.
- Formalize AI Builder/Copilot prompt governance for transcript-to-minutes workflows.
- Formalize SharePoint/Teams document storage conventions.
- Add deployment profiles for local placeholders, current Power Platform development space, and future Azure/Entra-enabled environments.
- Design Azure/Entra integration boundaries now, but do not require live Azure/Entra implementation until credentials, tenant access, and governance approvals exist.
- Add governance and reporting as an explicit implementation workstream, from Dataverse views through future Power BI/Fabric.
- Reference the available skills, agents, CLI tools, and missing modules that should shape implementation sequencing.

## Additional Improvements

- Add a solution-component manifest that maps every table, choice, flow, app screen, environment variable, security role, and test suite to a source artifact.
- Add a branch and release strategy for unmanaged development, test import, and future managed deployment.
- Add a maker handoff guide for Power Platform Studio steps that cannot yet be generated reliably from CLI.
- Add a data-retention and records-management appendix for minutes, agendas, papers, decisions, actions, transcripts, and AI outputs.
- Add data-loss-prevention assumptions and connector policy checks for Outlook, Teams, Planner, SharePoint, AI Builder, Graph, and future Azure services.
- Add audit logging conventions for AI-generated minutes, human review, approvals, meeting publication, access-rule changes, and integration retries.
- Add an accessibility and mobile/Teams usability checklist for the meeting workspace.
- Add migration notes for moving from dev-only Dataverse tables to a governed environment with service principals, managed identities, Graph permissions, and enterprise DLP policies.
- Add operational support runbooks for failed flows, stuck approvals, stale external IDs, orphaned Planner tasks, missing files, and broken attendee resolution.
- Add release-readiness gates for solution checker, validation scripts, smoke tests, maker handoff review, data-protection review, and rollback notes.
- Add business-continuity assumptions for exporting agendas/minutes/decision registers if a connector or environment is unavailable.
- Add a reporting data dictionary and metric definitions so dashboard numbers are consistent across app views and future Power BI.

## Acceptance Criteria

- A real unmanaged solution can be unpacked, validated, and repacked from source.
- Dataverse schema build scripts can run in dry-run mode and, when explicitly confirmed, against a disposable development environment.
- One end-to-end flow is live in development, preferably Outlook/Teams invite sync into `Meeting` and `Attendee`.
- A minimal app surface supports creating a governance body, meeting series, meeting, agenda items, papers, minutes, decisions, and actions.
- Security roles are represented in source and mapped to governance personas.
- Seed data can populate a representative governance structure for testing.
- Validation catches missing relationship targets, duplicate logical names, missing app coverage, missing flow environment variables, and stale generated checklists.
- Azure/Entra integration design exists with implementation gates, required permissions, and tenant prerequisites, but no live Azure/Entra dependency is introduced into the MVP.
- Governance reporting is represented in source, including Dataverse views/dashboard intent, future Power BI/Fabric model boundaries, security assumptions, and core metrics.
- Tooling coverage references `pac`, `pacx`, XrmToolBox, Git, GitHub CLI, Node/npm, .NET, PowerShell, relevant skills, worker/explorer agents, and missing PowerShell modules.
- Operational support and release-governance runbooks exist before any shared development or test environment release.
