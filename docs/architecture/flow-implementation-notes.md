# Flow Implementation Notes

These notes define the source-ready scaffold shape for Decisions Workspace flows under `powerplatform/src/flows`.

## Common Contract

Each flow spec should carry the same top-level sections:

- `schema`
- `flow`
- `trigger`
- `inputs`
- `outputs`
- `idempotency`
- `connectorCalls`
- `steps`
- `testHooks`

The goal is to make the flow source readable before it becomes a packaged Power Automate artifact.

## Shared Requirements

- Use Dataverse as the system of record.
- Read the source row first, then decide whether work is needed.
- Guard against duplicate runs with an external ID, sync hash, or stored post/task/event ID.
- Write an `dft_IntegrationLog` row for success, skip, and failure paths.
- Store external connector IDs back on the authoritative Dataverse record.
- Keep feature flags explicit for anything optional or future-only.
- Keep Teams, Planner, and SharePoint connector bindings environment-driven in production; only use discovered local binding candidates in a dev space when the flow spec marks them optional.
- Keep Graph-only work out of the MVP scaffolds unless the flow is clearly marked as future work.
- Use child scopes named `Try`, `Catch`, and `Finally` in Power Automate so success, handled failure, and cleanup paths are visible during maker review.
- Keep outbound side effects behind explicit source flags such as sync requested, invite requested, post requested, or processing requested.
- Write a replay-safe correlation key before invoking external connectors where the connector supports duplicate-prone operations.

## Flow By Flow

### Planner Task Sync

- Trigger on `dft_ActionItem` create or update.
- Skip when `dft_PlannerTaskId` already exists or the sync hash matches the last successful run.
- Gate the Planner connector behind `decisions_PlannerPlanId`; in a local dev space the DMS Workplan plan id `fictional-planner-plan` can be used as the optional binding candidate.
- Create a Planner task only once, then update the task on later edits.
- Write the Planner task ID and sync state back to the action row.
- Required maker controls:
  - Confirm the source action is still open and assigned before creating a task.
  - Map priority and due date consistently; do not create Planner tasks with missing owners unless the action explicitly permits unassigned work.
  - Store the last outbound hash after a successful create or update, not before the connector call.
- Acceptance evidence:
  - First run creates exactly one task.
  - Second run with unchanged data logs `Skipped`.
  - Title or due-date edits update the existing task and preserve `dft_PlannerTaskId`.

### Outlook Invite Sync

- Trigger on meeting invite requests or attendee changes.
- Skip when the meeting already has an Outlook event ID and the invite hash has not changed.
- Create or update the calendar event depending on whether the meeting already has an event ID.
- Store the Outlook event ID and invite sync state on the meeting row.
- Required maker controls:
  - Resolve attendees from `dft_Attendee` rows rather than free-text fields where possible.
  - Exclude declined, inactive, or observer-only attendees unless the meeting type explicitly includes them.
  - Write invite sync status back to the meeting and write attendee resolution failures to `dft_IntegrationLog`.
- Acceptance evidence:
  - Draft meeting with valid attendees creates one event.
  - Re-run with no meeting changes logs `Skipped`.
  - Attendee or time changes update the same event ID.

### Teams Summary Post

- Trigger on approved minutes or scheduled action digests.
- Skip if the current summary key was already posted.
- Post to the configured Teams team and channel from environment variables.
- Keep the Teams team binding optional in local dev only; the DMS Executive Team group id `11111111-1111-4111-8111-111111111111` is the candidate binding and `https://example.invalid/sites/decisions` is the deferred SharePoint site candidate.
- Store the Teams message ID and posting key on the meeting row.
- Required maker controls:
  - Only post approved or explicitly preview-marked summaries.
  - Use a deterministic summary key built from meeting ID, post type, approval state, and version.
  - Keep confidential paper titles and restricted transcript content out of channel posts.
  - Refuse to post when the AI-assisted minutes review state has not been recorded in Dataverse.
- Acceptance evidence:
  - Preview mode writes a log without posting externally.
  - Approved post writes one Teams message and stores the message ID.
  - Re-run with the same key logs `Skipped`.

### Org Hierarchy Sync

- Trigger manually or on a scheduled refresh.
- Resolve profile, manager, and direct-report data through Office 365 Users.
- Upsert `dft_OrgPerson` and `dft_OrgRelationship` by object ID, not display name.
- Log the sync result for later audit and smoke tests.
- Required maker controls:
  - Treat display name, title, and department as cached values; object ID and mail are the stable keys.
  - Mark missing managers as handled warnings, not hard failures.
  - Deactivate stale relationships rather than deleting historical hierarchy rows.
- Acceptance evidence:
  - Manual refresh upserts the current user, manager, and direct reports.
  - Re-run updates the same rows.
  - Missing manager data creates a warning log and does not stop the rest of the sync.

### AI Transcript Minutes

- Enable only when the AI Builder feature flag is on.
- Treat prompt output as draft material until reviewed.
- Create draft minutes, candidate decisions, and candidate actions from transcript text.
- Store the AI Builder run and processing state before handing the result to a human reviewer.
- Log the review gate explicitly so the transcript flow is obviously draft-only until a reviewer accepts or edits the output.
- Required maker controls:
  - Refuse restricted or unsupported classifications unless the prompt is approved for that classification.
  - Store prompt name, prompt version, input hash, output hash, reviewer, and review outcome.
  - Never publish minutes, decisions, or actions directly from AI output; create draft records only.
  - Keep SharePoint, Teams, and Planner side effects deferred until the review state is present in Dataverse.
- Acceptance evidence:
  - Disabled feature flag logs `Skipped`.
  - Enabled sample transcript creates a run row and draft records.
  - Human review is required before minutes status can move to approved or published.

## Environment Variables

The scaffold set assumes these environment variables are available from deployment settings:

- `decisions_TeamsTeamId`
- `decisions_TeamsChannelId`
- `decisions_PlannerPlanId`
- `decisions_SharePointSiteUrl`
- `decisions_EnableAiBuilderMinutes`
- `decisions_AiBuilderPromptName`
- `decisions_IntegrationLogRetentionDays`

## Test Hooks

Every scaffold exposes a named test hook that can be mapped to a smoke scenario or manual validation step.

The preferred check is simple:

- run the flow once against seeded Dataverse data,
- run it again with the same source row,
- confirm the second run skips instead of duplicating the external side effect.

That pattern should hold for Planner tasks, Outlook events, Teams posts, hierarchy sync, and AI Builder runs.
