# Implementation Handoff

This handoff translates the Teams-hosted Decisions Workspace MVP into buildable source artifacts for the maker and developer working in Power Apps.

## Source Artifacts

- [powerplatform/src/app/app-map.yaml](../../powerplatform/src/app/app-map.yaml)
- [powerplatform/src/app/components.yaml](../../powerplatform/src/app/components.yaml)
- [powerplatform/src/app/commands.yaml](../../powerplatform/src/app/commands.yaml)
- [powerplatform/src/app/access-matrix.yaml](../../powerplatform/src/app/access-matrix.yaml)
- [powerplatform/src/app/screens.md](../../powerplatform/src/app/screens.md)
- [powerplatform/src/app/powerfx-patterns.md](../../powerplatform/src/app/powerfx-patterns.md)

## MVP Activation Path

Build the first usable experience as a guided record chain:

1. Governance body or committee: create the body, capture the terms of reference, set the parent body and quorum rule, and seed the active membership list.
2. Meeting: create the meeting from the body or series, assign the chair, set dates and notice details, and open the meeting workspace with `varSelectedMeeting` set.
3. Agenda: order agenda items, link papers, flag decision items, and keep agenda editing anchored to the selected meeting.
4. Minutes: import or paste the transcript, stage draft minutes, and compare draft text to the agenda and transcript before review.
5. Decisions and actions: publish approved minutes, expose the linked decisions, and move open actions into the follow-up list and Planner sync flow.

## Build Order

1. Create the shell and navigation from `app-map.yaml`.
2. Implement reusable app parts from `components.yaml`.
3. Wire the command bar from `commands.yaml`.
4. Apply access control and row-level filtering from `access-matrix.yaml`.
5. Bind each screen to the listed Dataverse tables and variables in `screens.md` and `powerfx-patterns.md`.

## Reusable Components

- `shell_header`: Teams-style app header with global actions and selection context.
- `role_chip_strip`: effective role and scope indicator.
- `summary_kpi_row`: compact operational summary for meetings, reviews, and backlog.
- `record_list_panel`: gallery shell for list-led screens.
- `record_form_panel`: form shell for single-record editing with related children.
- `status_badge`: normalized status rendering across records.
- `quorum_indicator`: quorum requirement and attainment display.
- `transcript_import_panel`: transcript capture and draft launch surface.
- `review_compare_panel`: comparison surface for transcript, agenda, minutes, decisions, and actions.
- `sync_state_panel`: Planner sync and integration log surface.

## Command Bar

The command bar should be role-aware and context-aware.

- Global: `New meeting`, `Import transcript`, `Refresh people`
- Meeting work: `New agenda item`, `Confirm quorum`, `Publish minutes`, `Sync actions`
- Review work: `Draft minutes`
- Admin and audit: `Review audit log`

Disable commands when:

- no parent record is selected
- the current role is not allowed
- the record is already published or closed
- Planner sync is disabled

## Access Model

Use screen hiding plus row-level filtering.

- Governance administrators: full control.
- Secretariat: read-write for operational records.
- Chair: read-write for chaired meeting records and approvals.
- Member: read-only for assigned scope.
- Guest presenter: narrowly scoped access to a meeting or paper.
- Auditor: read-only access to approved records and logs.

## Screen-to-Data Binding Notes

### HomeScreen

- Bind summary cards to counts from `dft_Meeting`, `dft_ActionItem`, `dft_Minutes`, and `dft_IntegrationLog`.
- Scope galleries from the current person and selected body.

### GovernanceBrowseScreen and GovernanceEditScreen

- Use `dft_GovernanceBody`, `dft_TermsOfReference`, `dft_CommitteeMembership`, and `dft_MeetingSeries`.
- Keep the selected body in `varSelectedBody` before opening edit or related meeting views.
- Treat body save as the activation point for the meeting series and the default quorum context.

### MeetingListScreen and MeetingWorkspaceScreen

- Bind the list to `dft_Meeting`.
- Bind agenda, attendance, papers, decisions, and actions to the selected meeting.
- Keep `varSelectedMeeting` as the parent context for all child galleries and forms.
- Treat the meeting workspace as the handoff point from body setup into agenda, transcript, minutes, and follow-up actions.

### AgendaEditorScreen

- Bind ordered rows to `dft_AgendaItem`.
- Use `Sequence` as the ordering key.
- Keep linked papers and presenters visible in the edit surface.
- Make a linked paper or decision flag visible immediately after an agenda item is saved.

### PaperRegisterScreen

- Bind to `dft_Paper`.
- Surface the agenda lookup and file or link field together.
- Treat classification as a first-class filter and display value.

### AttendanceScreen

- Bind to `dft_Attendee`.
- Recalculate quorum from `dft_CommitteeMembership` and current attendance state.
- Show proxy and voting eligibility in the same record view.

### TranscriptImportScreen

- Bind the raw transcript input to the selected meeting.
- Store the imported text in `dft_Transcript`.
- Start AI draft staging through `dft_AIBuilderRun`.

### MinutesReviewScreen

- Bind draft minutes to `dft_Minutes`.
- Show candidate decisions and actions as derived review sets before promotion.
- Require a human review state before publish.
- Keep the chair review step explicit before decisions and actions are promoted.

### DecisionsActionsScreen

- Bind decisions to `dft_Decision`.
- Bind actions to `dft_ActionItem`.
- Mirror sync results into `dft_IntegrationLog`.
- Keep Planner sync separate from the publish step so actions can be reviewed first.

### SettingsScreen

- Bind defaults, readiness, and logs to `dft_IntegrationLog` plus the app configuration tables.
- Keep this screen read-heavy and operational.

## Implementation Notes

- Dataverse is the source of truth.
- Teams, Planner, SharePoint, and AI Builder are integration layers.
- Minutes, decisions, and actions must not move to published state without explicit human review.
- Keep the first implementation usable without custom Teams meeting extensions.
