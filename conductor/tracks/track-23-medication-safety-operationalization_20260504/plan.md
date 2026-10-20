# Track 23 Plan

## Workstreams

1. Guide pack and Teams setup
   - Add a quick-start guide for opening the model-driven app, adding it to Teams as a tab, and using the main navigation.
   - Add the manual Teams Website tab fallback because Graph/Teams automation is still authentication-gated.
   - Add a first-run checklist for chair, secretariat, member, and administrator roles.

2. Decisions feature coverage and unused-surface audit
   - Maintain a feature coverage matrix that maps public Decisions-style capabilities to Dataverse tables, app navigation, flow contracts, reporting, and status.
   - Mark each feature as implemented, scaffolded, planned, blocked, or not applicable.
   - Identify components that are built but not yet surfaced in the model-driven app or guide.

3. Medication Safety Committee operating model
   - Define the committee, governance body, terms of reference, membership, roles, and review cadence.
   - Treat Renee as secretariat and the user as chair once real NSW-source records are confirmed.
   - Keep local sample data synthetic until NSW Outlook/SharePoint access is available.

4. NSW Outlook and document-source gate
   - Stop using Gmail for committee source material.
   - Use an Outlook Email connector, Graph mailbox search, SharePoint/Teams files, or user-provided exported attachments when available.
   - Record source, source date, confidentiality status, and import reviewer before any real record becomes sample data.

5. AI Builder minutes and human review
   - Use transcript, agenda, attendee, paper, and prior-action context as bounded inputs.
   - Store AI Builder run metadata and generated draft outputs.
   - Require secretariat review and chair approval before minutes, decisions, or actions are published.
   - Keep prompt version, input hash, output hash, reviewer, and status in the audit trail.

6. Medication Safety workflows
   - Outlook invite sync creates or updates meeting and attendee records.
   - Agenda and paper intake creates agenda item and paper records with SharePoint links.
   - Transcript import creates transcript records and optionally queues AI Builder drafting.
   - Human review creates approved minutes, decisions, and action items.
   - Planner sync creates tasks only for approved action items when the meeting switch enables Planner.
   - Teams post publishes an approved summary only when enabled.
   - Reporting shows overdue actions, open decisions, missing papers, unreviewed AI outputs, and terms-of-reference review dates.

7. ALM and deployment readiness
   - Reconcile `powerplatform/settings/environment-variable-manifest.json` with `powerplatform/settings/deployment-settings.sample.json`.
   - Add or generate source-controlled solution environment variable definitions for the `decisions_*` settings.
   - Re-run full settings import after environment variables exist in the solution.
   - Keep Azure/Entra service principals designed but disabled until tenant access exists.

## Parallelization

- Agent A: guide pack, Teams setup, and user workflow.
- Agent B: feature coverage matrix and unused-surface audit.
- Agent C: Medication Safety Committee operating model and synthetic sample records.
- Agent D: AI Builder prompt contract, review gate, and audit pattern.
- Agent E: ALM environment-variable reconciliation and settings import readiness.
- Agent F: reviewer pass over terminology, privacy, and NSW-source gates.

## Current Blockers

- No callable Outlook Email tool is exposed in the current session.
- Graph/Teams authentication has been unstable in prior attempts.
- Real Medication Safety Committee documents must come from NSW Outlook, SharePoint, Teams, or user-provided exports, not Gmail.
- AI Builder licensing/capacity must be confirmed before enabling live minute drafting.
- Full deployment settings import is blocked until matching environment variable definitions exist in the solution.

## Done Definition

- Track 23 artifacts are present and referenced from Conductor status and track index.
- Guide, feature coverage, AI Builder, and Medication Safety workflow docs are present.
- Repo validation passes.
- Real-data ingestion remains gated and documented.
- Remaining live deployment blockers are explicitly separated from local build completeness.
