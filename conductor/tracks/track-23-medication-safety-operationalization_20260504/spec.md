# Track 23: Medication Safety Operationalization

## Objective

Turn the current Decisions Workspace scaffold into a usable governance workspace for the Medication Safety Committee while keeping real NSW email and document data behind explicit access and privacy gates.

This track also closes the usability gap raised after the app was published: the model-driven app needs a practical guide, feature coverage needs to be explicit, AI Builder needs a safe operating pattern, and the components already built need to be used by a real committee workflow rather than remaining disconnected tables.

## Scope

- Create a user guide for adding and using the Decisions Workspace in Teams.
- Map public Decisions-style capabilities to current repo implementation, source artifacts, and remaining gaps.
- Define the Medication Safety Committee operating model, including chair, secretariat, agenda, papers, minutes, actions, decisions, approvals, and reporting.
- Define how Renee as secretariat can operate the meeting workflow once the NSW Outlook or SharePoint source is available.
- Define AI Builder usage for transcript-to-minutes and decision/action extraction with a mandatory human review gate.
- Keep sample records synthetic or redacted until NSW Outlook/SharePoint access is available.
- Add ALM acceptance criteria for source-controlled environment variable definitions so full settings import can succeed.

## Non-Goals

- Do not search Gmail for NSW Health committee records.
- Do not ingest real NSW email, agendas, minutes, papers, transcripts, or personal information until an NSW Outlook/SharePoint source is available and authorized.
- Do not auto-publish AI-generated minutes, decisions, or actions.
- Do not implement Azure/Entra service-principal automation until tenant access and approvals are available.

## Decisions Feature Coverage

The app should continue to cover:

- Meeting series and individual meetings.
- Agendas, papers, minutes, decisions, actions, attendees, apologies, quorum, votes, and conflicts.
- Committee, governance body, terms of reference, membership, and reporting-line records.
- Per-meeting feature switches for voting, transcript import, AI minutes, Planner sync, Teams posts, SharePoint storage, Outlook invites, and org hierarchy.
- Integration logs and review status for connector, Graph, Planner, Teams, SharePoint, Outlook, and AI Builder paths.

The next implementation wave should make the built surface coherent by adding a guided home/dashboard, committee-specific setup records, and workflow documentation that shows where each table and flow contract is used.

## AI Builder Position

AI Builder is draft assistance only. It can:

- Draft minutes from a transcript, agenda, attendance record, and paper register.
- Extract candidate decisions, actions, risks, deferrals, and unresolved items.
- Compare a draft minute against the agenda to flag missing agenda items.
- Produce a review checklist for the chair and secretariat.

AI Builder must not:

- Approve minutes.
- Mark decisions as official.
- Publish to Teams or SharePoint without human approval.
- Create Planner tasks without review when the meeting feature switch disables Planner sync.

## Live Data Gate

NSW email is the correct source for Renee and the Medical or Medication Safety Committee material. The current exposed app connector set does not provide a callable Outlook Email search tool, so this track treats NSW Outlook/SharePoint ingestion as blocked until one of these is available:

- Outlook Email connector tooling in the agent session.
- Microsoft Graph mailbox search that authenticates reliably for the NSW account.
- A SharePoint or Teams file source for agendas, papers, and minutes.
- User-provided exported attachments placed into the repo or a known workspace path.

## Acceptance Criteria

- A track folder exists with `metadata.json`, `spec.md`, and `plan.md`.
- User guide, feature coverage, AI Builder pattern, and Medication Safety workflow docs are committed.
- The docs clearly state that NSW Outlook, not Gmail, is the source for real committee records.
- Synthetic Medication Safety sample records are documented separately from any real data import.
- AI Builder has a documented prompt contract, review gate, and audit trail.
- The Medication Safety workflow maps meeting invites, attendees, agenda, papers, minutes, transcript, decisions, actions, Planner, Teams, SharePoint, terms of reference, access, reporting, and per-meeting feature switches.
- The plan includes an ALM task to reconcile `decisions_*` environment variable settings with source-controlled solution environment variable definitions.
