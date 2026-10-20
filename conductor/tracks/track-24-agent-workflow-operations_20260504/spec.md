# Track 24: Agent Workflow Operations

## Objective

Make the Decisions Workspace build process easier to run with multiple agents by defining stable ownership lanes, reviewer pairings, live-deployment rules, and a repeatable Dylan verification script.

This track exists because the workflow now spans source scaffolding, generated app pages, Dataverse schema, seed data, live Dylan deployment, Teams/Planner/SharePoint/Outlook gates, and Medication Safety Committee operationalization. Parallel agents are useful, but only when their write sets and live side effects are constrained.

## Scope

- Define the recommended agents for the workflow and the files each lane owns.
- Define when to use writer/reviewer pairs.
- Keep live Power Platform operations serialized through one deployment operator.
- Add a Dylan verification helper that checks the app module, generated pages, key tables, and optional Medication Safety sample records.
- Add evidence expectations for each agent handoff.
- Keep real NSW committee data gated until Outlook, SharePoint, Teams, or user-provided source files are available.

## Non-Goals

- Do not create autonomous agents with persistent production write access.
- Do not run parallel live solution imports into Dylan.
- Do not ingest real agendas, minutes, transcripts, or papers from non-NSW sources.
- Do not treat generated pages, PAC/PACX output, or local artifacts as proof that Dylan changed unless a live verification command confirms it.

## Agent Lanes

Use these lanes for parallel implementation:

- Power Platform ALM Agent: solution pack/import/export, solution checker, GitHub Actions, releases, ALM profiles, connection references, and deployment settings.
- Model-Driven App UX Agent: sitemap, generated pages, forms/views, in-app guides, app navigation, and user workflow polish.
- Dataverse Data Model Agent: schema, relationships, choices, seed data, import/upsert scripts, and data-quality checks.
- Workflow Integration Agent: Planner, Teams, SharePoint, Outlook, Office 365 Users, AI Builder, Graph, and flow contracts.
- Governance and Safety Agent: access controls, DLP, privacy, NSW source rules, audit, human review, and real-versus-synthetic data separation.
- Medication Safety Secretariat Agent: agenda, papers, minutes, attendees, decisions, actions, terms of reference, and committee-specific workflow records.
- Testing and Review Agent: repo validation, solution checker, smoke tests, app verification, and risk review.
- Deployment Operator Agent: Dylan deployment, generated page upload, environment verification, and deployment evidence.

## Acceptance Criteria

- `docs/operations/agent-runbook.md` exists and lists agent ownership, tools, write boundaries, review pairs, and live deployment rules.
- `scripts/Verify-DylanDeployment.ps1` exists and can run read-only checks against Dylan.
- Track 24 is referenced from Conductor status, track index, roadmap, source map, and planning validation.
- Repo validation passes.
- The runbook explicitly states that live Dylan imports are serialized through one deployment operator.
- The runbook explicitly states that real Medication Safety Committee source records remain gated on NSW Outlook, SharePoint, Teams, or user-provided exports.
