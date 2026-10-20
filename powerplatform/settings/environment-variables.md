# Environment Variables

The values below are safe local examples, not secrets. Keep them in the sample deployment settings file until you are ready to bind a real tenant.

| Name | Purpose | MVP Source | Example value |
| --- | --- | --- | --- |
| `decisions_TeamsTeamId` | Teams team used for app/channel context | Manual setting | `00000000-0000-0000-0000-000000000000` |
| `decisions_TeamsChannelId` | Channel for governance notifications | Manual setting | `00000000-0000-0000-0000-000000000000` |
| `decisions_PlannerPlanId` | Planner plan for action sync | Manual setting | `planner-plan-id` |
| `decisions_SharePointSiteUrl` | Agenda, paper, and minutes storage | Manual setting | `https://contoso.sharepoint.com/sites/decisions-workspace` |
| `decisions_MinutesLibraryName` | Document library used for meeting records | Manual setting | `Shared Documents` |
| `decisions_EnableAiBuilderMinutes` | Feature flag for transcript-to-minutes | Manual setting | `false` |
| `decisions_EnableGraphTranscriptSync` | Future Graph transcript automation | Disabled in MVP | `false` |
| `decisions_AiBuilderPromptName` | AI Builder prompt for transcript-to-minutes drafting | Manual setting | `Decisions Workspace - Transcript to Minutes` |
| `decisions_IntegrationLogRetentionDays` | Default retention for integration logs | Manual setting | `90` |
