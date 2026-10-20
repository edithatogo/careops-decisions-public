# Track 13 Plan

1. Solution ALM hardening
   - Replace the placeholder solution with a real unmanaged solution exported from the development environment.
   - Track publisher, solution metadata, environment variables, connection references, tables, choices, apps, flows, security roles, and test assets.
   - Add pack/unpack validation to the repo validation script.

2. CLI command binding
   - Inventory installed `pac`, `pacx`, PowerShell modules, and Power Platform connector tooling.
   - Replace placeholder command templates in `Invoke-DataverseSchemaBuild.ps1` with confirmed syntax where supported.
   - Keep live execution behind explicit environment and confirmation switches.

3. Security and access
   - Define Dataverse roles for Governance Admin, Committee Secretary, Chair, Member, Paper Author, Restricted Reader, and Auditor.
   - Map row ownership, team ownership, access teams, business units, and SharePoint permissions.
   - Add validation that each sensitive table has an access strategy.

4. MVP app build
   - Build the first usable app surface around governance bodies, meeting series, meetings, agendas, papers, minutes, decisions, and actions.
   - Prefer a model-driven admin app plus a lightweight Teams/canvas workflow app.
   - Connect app screens back to the source app map and Power Fx patterns.

5. First real integration flow
   - Implement Outlook/Teams invite sync into Dataverse.
   - Resolve attendees against `OrgPerson`.
   - Preserve update idempotency using external meeting IDs and integration logs.
   - Add smoke tests for create, update, cancellation, attendee changes, and duplicate prevention.

6. Seed and test data
   - Add importable sample data for governance hierarchy, committees, members, reporting lines, meetings, agendas, papers, minutes, decisions, actions, and transcripts.
   - Add a reset/reseed runbook for the development environment.

7. Validation expansion
   - Add schema dependency validation.
   - Add relationship target validation.
   - Add duplicate logical-name validation.
   - Add app screen-to-table coverage validation.
   - Add flow connector and environment-variable coverage validation.
   - Add generated-artifact freshness checks.

8. AI Builder and Copilot governance
   - Define approved prompt records, expected JSON output schemas, human review states, and audit fields.
   - Keep transcript-to-minutes generation behind human review and publication workflow.
   - Add test fixtures for transcript summarization, decision extraction, action extraction, and hallucination checks.

9. Document storage and records management
   - Define SharePoint/Teams paths for agendas, papers, minutes, transcripts, restricted packs, and published records.
   - Define naming, versioning, retention, classification, and inheritance rules.
   - Map Dataverse records to document library metadata.

10. Deployment profiles
    - Maintain a local placeholder profile.
    - Add a Power Platform development-space profile.
    - Add a future Azure/Entra profile with required app registrations, Graph permissions, service principals, managed identities, DLP assumptions, and admin approvals.
    - Treat Azure/Entra profile as design-only until tenant access is available.

11. Governance and reporting
    - Add Dataverse views for chair, secretary, member, auditor, and administrator workflows.
    - Add app dashboard requirements for upcoming meetings, agenda readiness, minutes approval, decisions, overdue actions, integration failures, and audit review.
    - Define future Power BI/Fabric semantic model boundaries, row-level security approach, and metric definitions.
    - Add reporting to smoke scenarios and deployment validation.

12. Tooling, skill, and agent map
    - Keep `docs/tooling-inventory.md` current with installed CLI tools, versions, paths, missing PowerShell modules, relevant skills, and conductor agent roles.
    - Use `pac`/`pacx` first for live Power Platform work; use XrmToolBox for metadata inspection and query validation.
    - Use Power BI skills only when the reporting model moves from documented design to report implementation.
    - Use Azure and Entra skills only for design until access is granted.

13. Operational governance
    - Add release-readiness and rollback checklists.
    - Add support runbooks for failed flows, stale IDs, missing files, stuck approvals, AI review exceptions, and access-rule errors.
    - Define DLP, retention, sensitivity, audit, and evidence-capture assumptions.

## Parallelization

- Worker A: solution ALM and CLI command binding.
- Worker B: security roles, access model, and records-management conventions.
- Worker C: MVP app surface and Test Engine coverage.
- Worker D: first integration flow and flow smoke tests.
- Worker E: Azure/Entra design pack and implementation gates.
- Worker F: governance reporting, Dataverse views, app dashboards, Power BI/Fabric semantic model.
- Worker G: operational governance, support runbooks, DLP/retention/audit/release gates.

## Validation

Run:

- `scripts\Invoke-RepoValidation.ps1 -RequireImplementationScaffolds`
- `scripts\Test-ImplementationReadiness.ps1 -Strict`
- `scripts\Invoke-DataverseSchemaBuild.ps1 -PlanOnly`
- Future: reporting coverage validator once Dataverse views and dashboard source artifacts exist.
- Future: release-readiness validator once support and rollback runbooks exist.

Add new validators as each hardening item moves from planned to implemented.
