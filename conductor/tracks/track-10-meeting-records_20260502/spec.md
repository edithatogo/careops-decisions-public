# Track 10: Meeting Records

## Goal

Manage agendas, papers, minutes, decisions, actions, versions, approvals, and registers in a governance-ready workflow.

## Scope

- Meeting lifecycle: draft, planned, invited, in progress, minutes drafting, ready for review, approved, published, cancelled, deferred.
- Agenda templates with standing items, presenter, time allocation, purpose (information, discussion, decision, action, ratification, noting).
- Paper metadata: title, document source (SharePoint, Teams, email, uploaded, Dataverse file, external link), classification, status, SharePoint location fields (site, drive, item IDs).
- Minutes state machine: draft, chair review, approved, published, archived.
  - Draft: secretariat prepares working text.
  - Chair Review: submitted to chair for approval.
  - Approved: chair confirms, optional approval date and approver recorded.
  - Published: final version distributed, optional published URL and SharePoint location stored.
  - Archived: previous published versions retained for audit.
- Decision register: formal resolutions linked to meeting and agenda item, with decision number, authority, effective date, review due date, and status (proposed, resolved, deferred, rescinded, superseded).
- Action register: follow-up tasks with owner, due date, priority, Planner sync metadata, completion evidence, and status (open, in progress, blocked, completed, overdue, closed, cancelled).
- Document storage abstraction: Dataverse-first (columns for SharePoint IDs) with a documented SharePoint migration path.
- Pack generation: requirements for agenda packs, paper packs, and minutes packs as PDF or SharePoint collections.

## Acceptance Criteria

- Meeting records can support audit-ready governance workflows with full state machine transitions.
- Minutes states are documented with allowed transitions and approval gates.
- Decision and action registers provide register-style views (sequential numbering, status filtering, owner grouping).
- File storage is abstracted: Dataverse columns hold SharePoint references; a documented pattern exists for migrating to full SharePoint document libraries.
- Pack generation requirements are specified for later implementation.

## Out of Scope

- Live SharePoint document library creation (future DMS integration).
- Teams meeting scheduling from Dataverse (Outlook is the calendar authority).
- AI Builder prompt execution (Track 23 scope).
