# Flow Implementation Checklist

Generated checklist for the current Decisions Workspace flow catalog.

Source: `powerplatform/src/flows/flow-catalog.yaml`
Checklist source: `json` at `powerplatform/src/flows/implementation-checklist.json`

Use the generator to refresh this file from the repo state:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-FlowImplementationChecklist.ps1
```

## Flow Summary

| Flow | Connectors | Environment variables | Idempotency key | Test hook |
| --- | --- | --- | --- | --- |
| Planner Task Sync | Dataverse<br>Planner | decisions_PlannerPlanId<br>decisions_IntegrationLogRetentionDays | `dft_PlannerTaskId` | `planner-sync-manual-replay` |
| Outlook Invite Sync | Dataverse<br>Office 365 Outlook | decisions_TeamsChannelId<br>decisions_TeamsTeamId<br>decisions_IntegrationLogRetentionDays | `dft_OutlookEventId` | `outlook-invite-sync-dry-run` |
| Teams Summary Post | Dataverse<br>Microsoft Teams | decisions_TeamsTeamId<br>decisions_TeamsChannelId<br>decisions_IntegrationLogRetentionDays | `dft_LastSummaryPostKey` | `teams-summary-post-preview` |
| Org Hierarchy Sync | Dataverse<br>Office 365 Users | decisions_IntegrationLogRetentionDays | `dft_OrgPersonObjectId` | `org-hierarchy-sync-manual-refresh` |
| AI Transcript Minutes | Dataverse<br>AI Builder | decisions_EnableAiBuilderMinutes<br>decisions_AiBuilderPromptName<br>decisions_IntegrationLogRetentionDays | `dft_AiBuilderRunId` | `ai-transcript-minutes-sample-run` |

## Per-flow checklist

### Planner Task Sync

- File: `powerplatform/src/flows/planner-sync.yaml`
- Track: `track-06-automation-integrations_20260502`
- Purpose: Create and update Planner tasks for decision and action follow-through.
- Connectors: Dataverse, Planner
- Environment variables: decisions_PlannerPlanId, decisions_IntegrationLogRetentionDays
- Idempotency key: `dft_PlannerTaskId`
- Test hook: `planner-sync-manual-replay`

#### Maker implementation tasks
- [ ] Create or update the flow in Power Automate from `powerplatform/src/flows/planner-sync.yaml` without adding live write steps outside the intended business process.
- [ ] Configure the listed connectors and confirm the connection references match the environment.
- [ ] Bind the environment variables before test runs and keep values in deployment settings, not in the flow definition.
- [ ] Enforce idempotency with `dft_PlannerTaskId` so reruns do not duplicate side effects.
- [ ] Add a test path for `planner-sync-manual-replay` and verify it can be exercised without a live production change.

#### Test hook checklist
- [ ] Run `planner-sync-manual-replay` against sample or replay data.
- [ ] Confirm the run is repeatable and does not create duplicate records.
- [ ] Confirm connector calls and environment variable resolution match the catalog entry.
- [ ] Capture any maker notes needed for packaging or deployment review.

### Outlook Invite Sync

- File: `powerplatform/src/flows/outlook-invite-sync.yaml`
- Track: `track-11-invites-attendees_20260502`
- Purpose: Create or update Outlook calendar invites for meeting records.
- Connectors: Dataverse, Office 365 Outlook
- Environment variables: decisions_TeamsChannelId, decisions_TeamsTeamId, decisions_IntegrationLogRetentionDays
- Idempotency key: `dft_OutlookEventId`
- Test hook: `outlook-invite-sync-dry-run`

#### Maker implementation tasks
- [ ] Create or update the flow in Power Automate from `powerplatform/src/flows/outlook-invite-sync.yaml` without adding live write steps outside the intended business process.
- [ ] Configure the listed connectors and confirm the connection references match the environment.
- [ ] Bind the environment variables before test runs and keep values in deployment settings, not in the flow definition.
- [ ] Enforce idempotency with `dft_OutlookEventId` so reruns do not duplicate side effects.
- [ ] Add a test path for `outlook-invite-sync-dry-run` and verify it can be exercised without a live production change.

#### Test hook checklist
- [ ] Run `outlook-invite-sync-dry-run` against sample or replay data.
- [ ] Confirm the run is repeatable and does not create duplicate records.
- [ ] Confirm connector calls and environment variable resolution match the catalog entry.
- [ ] Capture any maker notes needed for packaging or deployment review.

### Teams Summary Post

- File: `powerplatform/src/flows/teams-summary-post.yaml`
- Track: `track-06-automation-integrations_20260502`
- Purpose: Post meeting and action summaries into the configured Teams channel.
- Connectors: Dataverse, Microsoft Teams
- Environment variables: decisions_TeamsTeamId, decisions_TeamsChannelId, decisions_IntegrationLogRetentionDays
- Idempotency key: `dft_LastSummaryPostKey`
- Test hook: `teams-summary-post-preview`

#### Maker implementation tasks
- [ ] Create or update the flow in Power Automate from `powerplatform/src/flows/teams-summary-post.yaml` without adding live write steps outside the intended business process.
- [ ] Configure the listed connectors and confirm the connection references match the environment.
- [ ] Bind the environment variables before test runs and keep values in deployment settings, not in the flow definition.
- [ ] Enforce idempotency with `dft_LastSummaryPostKey` so reruns do not duplicate side effects.
- [ ] Add a test path for `teams-summary-post-preview` and verify it can be exercised without a live production change.

#### Test hook checklist
- [ ] Run `teams-summary-post-preview` against sample or replay data.
- [ ] Confirm the run is repeatable and does not create duplicate records.
- [ ] Confirm connector calls and environment variable resolution match the catalog entry.
- [ ] Capture any maker notes needed for packaging or deployment review.

### Org Hierarchy Sync

- File: `powerplatform/src/flows/org-hierarchy-sync.yaml`
- Track: `track-08-microsoft-platform-integration_20260502`
- Purpose: Cache Office 365 Users profile and reporting hierarchy data in Dataverse.
- Connectors: Dataverse, Office 365 Users
- Environment variables: decisions_IntegrationLogRetentionDays
- Idempotency key: `dft_OrgPersonObjectId`
- Test hook: `org-hierarchy-sync-manual-refresh`

#### Maker implementation tasks
- [ ] Create or update the flow in Power Automate from `powerplatform/src/flows/org-hierarchy-sync.yaml` without adding live write steps outside the intended business process.
- [ ] Configure the listed connectors and confirm the connection references match the environment.
- [ ] Bind the environment variables before test runs and keep values in deployment settings, not in the flow definition.
- [ ] Enforce idempotency with `dft_OrgPersonObjectId` so reruns do not duplicate side effects.
- [ ] Add a test path for `org-hierarchy-sync-manual-refresh` and verify it can be exercised without a live production change.

#### Test hook checklist
- [ ] Run `org-hierarchy-sync-manual-refresh` against sample or replay data.
- [ ] Confirm the run is repeatable and does not create duplicate records.
- [ ] Confirm connector calls and environment variable resolution match the catalog entry.
- [ ] Capture any maker notes needed for packaging or deployment review.

### AI Transcript Minutes

- File: `powerplatform/src/flows/ai-transcript-minutes.yaml`
- Track: `track-12-ai-transcripts-minutes_20260502`
- Purpose: Turn transcript text into reviewed draft minutes, decisions, actions, and risks.
- Connectors: Dataverse, AI Builder
- Environment variables: decisions_EnableAiBuilderMinutes, decisions_AiBuilderPromptName, decisions_IntegrationLogRetentionDays
- Idempotency key: `dft_AiBuilderRunId`
- Test hook: `ai-transcript-minutes-sample-run`

#### Maker implementation tasks
- [ ] Create or update the flow in Power Automate from `powerplatform/src/flows/ai-transcript-minutes.yaml` without adding live write steps outside the intended business process.
- [ ] Configure the listed connectors and confirm the connection references match the environment.
- [ ] Bind the environment variables before test runs and keep values in deployment settings, not in the flow definition.
- [ ] Enforce idempotency with `dft_AiBuilderRunId` so reruns do not duplicate side effects.
- [ ] Add a test path for `ai-transcript-minutes-sample-run` and verify it can be exercised without a live production change.

#### Test hook checklist
- [ ] Run `ai-transcript-minutes-sample-run` against sample or replay data.
- [ ] Confirm the run is repeatable and does not create duplicate records.
- [ ] Confirm connector calls and environment variable resolution match the catalog entry.
- [ ] Capture any maker notes needed for packaging or deployment review.

## Safe verification order

1. Review the flow catalog and checklist output.
2. Configure connectors and environment variables in the maker environment.
3. Validate idempotency and test hook behavior with sample data.
4. Package only after the local checklist is complete.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-FlowImplementationChecklist.ps1 -DryRun
```
