# Screens

This is the canvas app screen contract for the Dataverse-first core. Teams, SharePoint, and Planner are deferred bindings rather than runtime requirements for the core screens.

Supporting contracts:
- [state-transitions.yaml](state-transitions.yaml) — complete lifecycle state machine for all entities
- [flow-contract-matrix.yaml](flow-contract-matrix.yaml) — cross-reference between app commands and flow contracts
- [app-map.yaml](app-map.yaml) — screen map, navigation, forms, galleries, activation path, build modes

## MVP Activation Sequence

The app should move through the record chain in this order:

1. Select or create the governance body and confirm its terms of reference, membership, access rules, and quorum rule.
2. Review the org hierarchy and meeting series for the selected body.
3. Create the meeting from the body or series and keep that meeting as the active parent context.
4. Resolve the per-meeting feature switchboard, then build the agenda, link papers, and mark which items expect decisions or votes.
5. Import the transcript, draft minutes, and stage candidate decisions, votes, and actions for review.
6. Publish approved minutes, then manage decisions and follow-up actions from the meeting workspace.
7. Inspect AI runs and integration logs when review or audit evidence is needed.

## HomeScreen

Purpose: landing page for the current user, organized around the same Meetings, Governance, Actions, Decisions, Papers, and Help headings used in the guide and shell contract.

Shows:
- Workflow launch cards for Prepare meeting, Build agenda, Add papers, Confirm attendance, Draft minutes, Decisions and actions, Configure committee, Feature switchboard, Review AI drafts, and Check integrations.
- My committees and governance bodies.
- My meetings today and this week.
- Pending reviews: agendas, minutes, decisions, votes, and actions.
- Overdue items I own or chair.
- Deferred binding status for Teams, SharePoint, and Planner.
- Quick actions for create meeting, record vote, import transcript, draft minutes, and refresh people.
- Setup health for required environment variables, connection references, feature switches, and source-data gates.

Controls:
- Summary cards at the top.
- `galHomeLaunchCards`
- `galMyMeetings`
- `galMyTasks`
- `galPendingReviews`
- `galSetupHealth`
- command bar with New, Refresh, Import, Vote, Draft, and Settings.

Navigation:
- Opens `MeetingListScreen` to prepare or create a meeting.
- Opens `AgendaEditorScreen` to build or update the agenda.
- Opens `PaperRegisterScreen` to register papers.
- Opens `AttendanceScreen` to confirm attendees, apologies, proxies, and quorum.
- Opens `MinutesReviewScreen` to draft or review minutes.
- Opens `DecisionsActionsScreen` to manage decisions and follow-up actions.
- Opens `GovernanceBrowseScreen` for body maintenance.
- Opens `MeetingWorkspaceScreen` for the selected meeting.
- Opens `TranscriptImportScreen` when transcript work is requested.
- Opens `SettingsScreen` for the feature switchboard and setup health.
- Opens `AccessRulesScreen`, `AIRunsScreen`, or `IntegrationLogsScreen` for review and audit work.
- Opens the generated quick-start guide as the Help surface for navigation and onboarding.

## GovernanceBrowseScreen

Purpose: browse governance bodies, committees, org hierarchy, terms of reference, and access rules.

Shows:
- Governance body gallery on the left.
- Committee gallery filtered to the selected body.
- Org hierarchy and reporting-line panel.
- Terms of reference summary.
- Membership summary.
- Access rule summary.
- Active meeting series.

Controls:
- `galGovernanceBodies`
- `galCommittees`
- `galMembership`
- `galMeetingSeries`
- `galAccessRules`
- search box and status filter.

Primary actions:
- New governance body.
- New committee.
- Edit terms of reference.
- Add member.
- Review access rules.
- Open meetings.

## OrgHierarchyScreen

Purpose: review parent and child governance links and org person relationships.

Shows:
- Governance tree.
- Parent body links.
- Reporting lines.
- Active and inactive org relationships.

Controls:
- `galHierarchy`
- `galOrgRelationships`
- `frmOrgRelationship`
- filter chips for body and relationship type.

Primary actions:
- Open parent body.
- Open child body.
- Add relationship.
- Deactivate relationship.

## GovernanceEditScreen

Purpose: maintain the selected body or committee record.

Shows:
- Edit form for body or committee subtype.
- Current terms of reference version.
- Membership subgallery.
- Related meeting series subgallery.
- Related access rules subgallery.

Controls:
- `frmGovernanceBody`
- `frmCommittee`
- `frmTermsOfReference`
- `galRelatedMembership`
- `galRelatedSeries`
- `galRelatedAccessRules`

Primary actions:
- Save.
- Save and new membership.
- Save and open meetings.
- Save and update access rules.

## MeetingListScreen

Purpose: find meetings by date, body, series, chair, and status.

Shows:
- Meeting list with status, quorum state, and vote state.
- Filters for body, series, month, and status.
- Search for meeting title or agenda topic.

Controls:
- `galMeetings`
- date picker
- status chips
- search input

Primary actions:
- New meeting from series.
- Open meeting workspace.
- Cancel meeting.

## MeetingWorkspaceScreen

Purpose: single meeting operational workspace.

Shows:
- Meeting header with status, body, chair, quorum, transcript state, and vote state.
- Feature switchboard panel for voting, AI minutes, transcript import, Planner sync, Teams posts, SharePoint document store, Outlook invites, and org hierarchy refresh.
- Agenda panel.
- Attendee panel.
- Papers panel.
- Votes panel.
- Conflicts panel.
- Minutes panel.
- Decisions and actions panel.
- AI run summary panel.
- Activation banner showing the current step in the body -> meeting -> agenda -> votes -> minutes -> decisions/actions chain.

Controls:
- `frmMeetingHeader`
- `galAgendaItems`
- `galAttendees`
- `galPapers`
- `galVotes`
- `galConflicts`
- `galDecisions`
- `galActions`
- tabs for Overview, Agenda, Attendance, Papers, Votes, Minutes, Decisions, Actions, Audit.
- `cmpMeetingFeatureSwitchboard`

Primary actions:
- Edit meeting details.
- Edit feature switchboard.
- Confirm quorum.
- Record vote.
- Import transcript.
- Draft minutes.
- Review access rules.

## AgendaEditorScreen

Purpose: create and reorder agenda items.

Shows:
- Ordered agenda gallery.
- Selected agenda item form.
- Linked papers and expected decisions.
- Vote trigger and decision flag controls.

Controls:
- `galAgendaEditor`
- `frmAgendaItem`
- move up / move down icons
- duplicate agenda item

Primary actions:
- Add item.
- Reorder item.
- Attach paper.
- Set presenter.
- Mark item complete.
- Mark vote required.

## PaperRegisterScreen

Purpose: register papers and bind them to meeting agenda items.

Shows:
- Paper gallery.
- Paper metadata form.
- File or link attachment area.
- Agenda item lookup.
- Deferred SharePoint document store placeholder.

Controls:
- `galPapers`
- `frmPaper`
- classification selector
- file picker or document store placeholder

Primary actions:
- Add paper.
- Update classification.
- Mark submitted or superseded.
- Link to agenda item.
- Stage document store binding later.

## VoteScreen

Purpose: capture votes and vote responses for a meeting or decision.

Shows:
- Vote gallery.
- Vote response gallery.
- Vote detail form.
- Result summary.

Controls:
- `galVotes`
- `galVoteResponses`
- `frmVote`
- `frmVoteResponse`
- result summary panel

Primary actions:
- Create vote.
- Record response.
- Close vote.
- Link outcome to decision.

## AttendanceScreen

Purpose: manage invitees, attendees, apologies, proxies, and quorum.

Shows:
- Expected attendees.
- Actual attendees.
- Attendance status.
- Quorum indicator and voting eligibility summary.

Controls:
- `galAttendees`
- `frmAttendee`
- quorum badge
- proxy selector

Primary actions:
- Resolve people.
- Record attendance.
- Record apology.
- Assign proxy.
- Recalculate quorum.

## TranscriptImportScreen

Purpose: ingest transcript text and prepare AI-assisted draft records.

Shows:
- Transcript source selector.
- Text input or paste area.
- Parsed agenda alignment preview.
- Processing status and AI run provenance.

Controls:
- `txtTranscript`
- `cmbTranscriptSource`
- `galTranscriptPreview`
- `btnParseTranscript`
- `btnDraftMinutes`

Primary actions:
- Paste transcript.
- Upload or link transcript file.
- Classify transcript.
- Launch draft generation.

## MinutesReviewScreen

Purpose: review draft minutes and chair approvals.

Shows:
- Draft minutes editor.
- Candidate decisions extracted from transcript.
- Candidate votes extracted from transcript.
- Candidate actions extracted from transcript.
- Comparison view against agenda, transcript, and AI run history.

Controls:
- `frmMinutes`
- `galDecisionCandidates`
- `galVoteCandidates`
- `galActionCandidates`
- review status selector
- comment box for chair edits

Primary actions:
- Accept draft.
- Revise text.
- Promote decisions.
- Promote votes.
- Promote actions.
- Send to chair review.

## DecisionsActionsScreen

Purpose: govern the outcome register.

Shows:
- Decisions gallery.
- Actions gallery.
- Ownership, due dates, and escalation state.
- Deferred Planner binding state.

Controls:
- `galDecisions`
- `galActions`
- `frmDecision`
- `frmAction`
- sync status filters

Primary actions:
- Confirm decision.
- Reassign action.
- Change status.
- Stage for Planner binding.

## AccessRulesScreen

Purpose: manage access rules for bodies, meetings, and papers.

Shows:
- Access rule gallery.
- Access rule editor.
- Audience scope summary.
- Active and inactive policies.

Controls:
- `galAccessRules`
- `frmAccessRule`
- audience scope chips

Primary actions:
- Add rule.
- Update scope.
- Deactivate rule.

## AIRunsScreen

Purpose: inspect AI builder runs and their provenance trail.

Shows:
- AI run gallery.
- AI run detail form.
- Review history.
- Linked transcript and minutes.

Controls:
- `galAIRuns`
- `frmAIRun`
- provenance panel
- source linkage panel

Primary actions:
- Open run details.
- Review outcome.
- Trace source record.

## IntegrationLogsScreen

Purpose: inspect integration and publication history.

Shows:
- Integration log gallery.
- Log detail view.
- Deferred binding events.
- Retry or replay outcomes.

Controls:
- `galIntegrationLogs`
- `frmIntegrationLog`
- correlation search

Primary actions:
- Inspect event.
- Review error.
- Record replay result.

## SettingsScreen

Purpose: administration and defaults.

Shows:
- Role mappings.
- Meeting templates.
- Classification defaults.
- Per-meeting feature switchboard defaults and inheritance.
- Deferred binding setup.
- Connector readiness summary.
- Integration logs.

Controls:
- `galSettingsSections`
- `galIntegrationLogs`
- `cmpMeetingFeatureSwitchboard`
- default body selector
- toggle switches for enabled features

Primary actions:
- Refresh people cache.
- Update feature switchboard defaults.
- Review logs.
- Adjust templates.
- Inspect connector failures.
- Review deferred bindings.

## Screen Transitions

- HomeScreen is the default landing screen.
- The home navigation headings are treated as collapsible groups in the shell contract.
- GovernanceBrowseScreen leads into GovernanceEditScreen, OrgHierarchyScreen, or MeetingListScreen.
- MeetingListScreen leads into MeetingWorkspaceScreen.
- MeetingWorkspaceScreen leads into AgendaEditorScreen, AttendanceScreen, PaperRegisterScreen, VoteScreen, TranscriptImportScreen, MinutesReviewScreen, DecisionsActionsScreen, and AccessRulesScreen.
- MinutesReviewScreen leads into AIRunsScreen when provenance needs review.
- SettingsScreen is always reachable from the command bar.

## Role Behavior

- Governance administrators see all screens and settings.
- Secretariats see creation, editing, import, voting, publication, and switchboard actions.
- Chairs see review and approval actions, with editing limited by configuration.
- Members see read-only or acknowledgement views for assigned bodies, meetings, papers, minutes, votes, decisions, and actions.
- Guests see only the meeting or paper they were invited to.
- Auditors see approved records, access rules, integration logs, and AI run history only.
