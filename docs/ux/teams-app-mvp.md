# Teams App MVP

This UX spec defines the first usable Teams experience for Decisions Workspace. The app is not a generic portal; it is a working meeting operations tool for governance bodies, meetings, agendas, papers, minutes, decisions, actions, attendees, transcript import, AI draft review, and settings.

## UX Principles

- Work inside Teams and keep the surface compact, readable, and fast to scan.
- Put the current meeting or body at the center of the screen.
- Make state visible: draft, reviewed, approved, published, overdue, and synced.
- Keep Dataverse as the source of truth; treat Teams, Outlook, Planner, SharePoint, and AI Builder as integration layers.
- Default to role-aware screens instead of duplicating content across separate apps.

## MVP Activation Path

The first usable flow must make the record chain explicit:

1. Create the governance body or committee, attach the terms of reference, set the parent body or reporting line, and record quorum and membership.
2. Create a meeting from the body or series, assign the chair, set the time and venue or Teams link, and open the meeting workspace as the active record.
3. Build the agenda in order, link papers, mark decision items, and use the meeting workspace as the control point for attendance and quorum.
4. Import the transcript, draft minutes from the meeting context, and compare the draft against the agenda and transcript before review.
5. Publish approved minutes, which exposes the linked decisions and opens the action items for follow-up and Planner sync.

## Primary Users

- Governance administrator: owns configuration, access, templates, and logs.
- Secretariat: prepares the meeting, manages the agenda pack, and maintains the records.
- Chair: reviews and approves agenda, minutes, decisions, and follow-up.
- Member: reads the meeting pack, attends, and tracks assigned actions.
- Guest or presenter: sees only the meeting or paper they were invited to.
- Auditor: reviews approved records and logs only.

## Shell Layout

- Left navigation rail for the main work areas.
- Top command bar for global actions.
- Main workspace that swaps between dashboard, list, and record pages.
- Right-side record inspector for detail work where the screen is list-led.

## Screen Flow

1. Open on the dashboard.
2. Select a governance body or meeting.
3. Work in the meeting workspace for agenda, papers, attendance, transcript, minutes, decisions, and actions.
4. Move into settings only when needed.

## Core Workflows

### Governance body setup

The user creates a governance body or committee, then attaches the current terms of reference and active membership. The workspace must show parent body, reporting line, quorum rule, classification, and active meeting series in one place. Secretariat users should be able to add members without leaving the body record.

### Meeting preparation

The secretariat creates a meeting from a series, confirms the chair, sets the start and end time, and checks attendee coverage. The meeting workspace should show:
- current status
- quorum state
- agenda progress
- paper count
- transcript state
- minutes state
- open actions
- the next activation step when the meeting is still in draft or ready-for-review state

### Agenda and papers

Agenda items are ordered. Each item should show presenter, purpose, time allocation, linked papers, and whether a decision is expected. Papers need classification, author, status, and file or link location. The interface should make linking a paper to an agenda item a one-step action.

### Attendance and quorum

Attendance needs a simple, explicit state model:
- invited
- accepted
- declined
- tentative
- present
- absent
- apology recorded
- proxy assigned

The chair and secretariat should see quorum immediately and should not have to infer it from counts.

### Transcript import

Transcript import is a separate step because it may come from pasted text, a file, or a later Graph integration. The import screen should show raw input, parse status, and a preview of what the app thinks the meeting content contains. If the transcript is not clean, the user must be able to keep the raw text and still proceed.

### AI draft review

AI output is draft assistance only. The review page should compare transcript text, agenda context, draft minutes, candidate decisions, and candidate actions. The human reviewer must explicitly accept or edit the draft before anything becomes official.

### Decisions and actions

Decisions should read like formal resolutions with authority, outcome, and owner. Actions should read like work items with due date, owner, status, and sync state. The app should let the user move from a decision to its linked actions and from an action back to the source meeting.

### Settings

Settings should be limited to operational defaults:
- default body and committee filters
- meeting templates
- classification labels
- connector readiness
- integration logs
- cached identity refresh

## Workspaces

### Dashboard

The dashboard is role-aware and should prioritize:
- meetings this week
- items needing review
- overdue actions
- meetings waiting on transcript or minutes
- governance bodies the user belongs to

### Governance workspace

The governance workspace combines body details, terms of reference, membership, and meeting series. It should support a browser-style flow: select body, inspect committee, then open the related meeting list.

### Meeting workspace

The meeting workspace is the main operational screen. It should support tabs or sections for:
- overview
- agenda
- attendance
- papers
- transcript
- minutes
- decisions
- actions
- audit

## Visual and Interaction Rules

- Use compact controls and restrained spacing.
- Use badges for status, not paragraphs.
- Use galleries for lists and forms for editing.
- Keep the current selection visible at all times.
- Make destructive actions rare and explicit.
- Keep edit surfaces one record deep unless the record is a parent with a small related list.

## Empty States

- No governance bodies: prompt to create the first body and terms of reference.
- No meetings: prompt to create a meeting from a series.
- No papers: prompt to register a paper and attach a file or link.
- No transcript: prompt to paste text or mark not expected.
- No actions: show that the meeting ended without follow-up items or that actions still need review.

## MVP Scope Boundary

The first version must be useful without Azure, Graph, or custom Teams meeting extensions. It should still leave room for those later capabilities by preserving stable concepts and record relationships.
