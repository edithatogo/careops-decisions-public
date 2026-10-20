# Transcript-to-Minutes Pipeline

## Purpose

Document the pipeline that transforms meeting transcript text into structured draft minutes, decisions, actions, risks, and unresolved items. The pipeline supports two paths: a manual MVP path and a future Graph-automated path.

## Pipeline Overview

```
                          ┌─────────────────────┐
                          │  Transcript Source   │
                          └──────────┬──────────┘
                                     │
                    ┌────────────────▼────────────────┐
                    │     Manual Paste / Upload       │
                    │  (MVP: dft_Source = Manual)    │
                    │  Graph Retrieval  (Future)      │
                    └────────────────┬────────────────┘
                                     │
                          ┌──────────▼──────────┐
                          │  dft_Transcript     │
                          │  dft_TranscriptText │
                          │  dft_Classification │
                          └──────────┬──────────┘
                                     │
                    ┌────────────────▼────────────────┐
                    │  AI Builder Prompt (Optional)   │
                    │  gated by decisions_            │
                    │  EnableAiBuilderMinutes         │
                    └────────────────┬────────────────┘
                                     │
                    ┌────────────────▼────────────────┐
                    │      AI Builder Run Row         │
                    │  dft_AIBuilderRun               │
                    │  Status: Completed              │
                    │  Review: Pending                │
                    └────────────────┬────────────────┘
                                     │
                    ┌────────────────▼────────────────┐
                    │     Draft Records Created       │
                    │  dft_Minutes   (Status: Draft)  │
                    │  dft_Decision  (Status: Draft)  │
                    │  dft_ActionItem(Status: Open)   │
                    │  dft_IntegrationLog: NeedsReview │
                    └────────────────┬────────────────┘
                                     │
                    ┌────────────────▼────────────────┐
                    │     Human Review Gate           │
                    │  Secretariat reviews/edit       │
                    │  Chair approves                 │
                    └────────────────┬────────────────┘
                                     │
                    ┌────────────────▼────────────────┐
                    │      Approved / Published       │
                    │  dft_Minutes: Approved          │
                    │  dft_Decision: Final            │
                    │  dft_ActionItem: Open/InProgress│
                    └─────────────────────────────────┘
```

## MVP Path (Manual Transcript)

### Step 1: Transcript Entry

1. Secretariat creates a `dft_Transcript` record linked to the meeting.
2. Transcript text is pasted or uploaded as `dft_TranscriptText`.
3. The transcript source is set to `Manual` and classification is set.
4. Optional: a file URL is stored for reference copies.

### Step 2: AI Builder Prompt (Optional)

1. The `ai-transcript-minutes` flow triggers when `dft_ProcessingRequested = true` and `dft_AiBuilderRunId` is empty.
2. The flow checks `decisions_EnableAiBuilderMinutes` feature flag.
3. If disabled, the flow logs a skip and stops.
4. If enabled, the flow resolves the prompt name from `decisions_AiBuilderPromptName`.
5. The prompt is invoked with the transcript text, meeting lookup, and classification.
6. Output is stored in a new `dft_AIBuilderRun` row.

### Step 3: Draft Record Creation

1. A draft `dft_Minutes` row is created with status `Draft`.
2. Candidate `dft_Decision` rows are created with status `Draft`.
3. Candidate `dft_ActionItem` rows are created with status `Open` and source type `Transcript`.
4. The transcript is updated: `dft_ProcessingState = 'NeedsReview'`.

### Step 4: Human Review Gate

1. The `dft_AIBuilderRun` row has `dft_HumanReviewStatus = 'Pending'`.
2. Secretariat reviews the draft minutes, decisions, and actions.
3. Secretariat edits, approves, or rejects each draft record.
4. The AI Builder run row is updated with `dft_ReviewedState`, `dft_ReviewerId`, and `dft_ReviewedOn`.
5. No AI-generated content reaches published minutes, Teams posts, or decision/action registers without human review evidence.

## Step 5: Approval and Publication

1. Chair reviews and approves the final minutes.
2. `dft_Minutes.dft_Status` moves from Draft → Approved.
3. With approved minutes, the `teams-summary-post` flow can post summaries.
4. With approved decisions and actions, the `planner-sync` flow can create Planner tasks.
5. Both flows check the human-review gate before proceeding.

## AI Builder Prompt Contract

### Prompt Inputs

| Input | Source | Type |
|---|---|---|
| Transcript text | `dft_Transcript.dft_TranscriptText` | Multiline text (max 32000 chars) |
| Meeting context | `dft_Meeting` lookup fields | Structured metadata |
| Classification | `dft_Transcript.dft_Classification` | Choice value |
| Agenda items | `dft_AgendaItem` rows linked to meeting | Structured list |

### Prompt Output Schema

The prompt must return JSON with the following structure:

```json
{
  "summary": "string — brief meeting summary",
  "agendaMinutes": [
    {
      "agendaItemTitle": "string",
      "minuteText": "string",
      "decisionSought": "boolean",
      "outcome": "string"
    }
  ],
  "candidateDecisions": [
    {
      "decisionText": "string",
      "agendaItemTitle": "string",
      "proposedStatus": "string"
    }
  ],
  "candidateActions": [
    {
      "actionSummary": "string",
      "proposedOwner": "string",
      "proposedDueDate": "string (ISO date)"
    }
  ],
  "risks": [
    {
      "riskDescription": "string",
      "severity": "string"
    }
  ],
  "deferredItems": [
    {
      "item": "string",
      "reason": "string"
    }
  ],
  "unresolvedItems": [
    {
      "item": "string",
      "notes": "string"
    }
  ],
  "missingEvidence": [
    "string — list of gaps in the transcript"
  ],
  "reviewChecklist": [
    "string — items the reviewer should verify"
  ]
}
```

### Validation Rules

1. The flow rejects output that is not parseable JSON.
2. The flow rejects output that omits `summary` or `reviewChecklist`.
3. The flow logs a warning if `candidateDecisions`, `candidateActions`, or `risks` are empty — the transcript may not have covered these items.
4. The output hash is computed and stored on the AI Builder run for audit.

## Human Review Gate Sequence

```
┌─────────────────────────────────────────────────────┐
│                  Human Review Gate                   │
├─────────────────────────────────────────────────────┤
│ 1. Secretariat opens the draft minutes              │
│ 2. Secretariat compares draft against:              │
│    - Agenda and papers                              │
│    - Transcript text                                │
│    - Attendance and apology records                 │
│    - Previous minutes and open actions              │
│ 3. Secretariat edits or accepts each section        │
│ 4. Secretariat records review in dft_AIBuilderRun   │
│ 5. Chair reviews final version                      │
│ 6. Chair approves → Minutes become official         │
│ 7. Approved record unlocks downstream flows:        │
│    - Teams summary posting                          │
│    - Planner task creation                          │
│    - SharePoint publication                         │
└─────────────────────────────────────────────────────┘
```

### Gate States

| State | Meaning | Action |
|---|---|---|
| Pending | AI output created, not yet reviewed | Secretariat should review |
| InReview | Secretariat actively reviewing | Edit in progress |
| Accepted | Output accepted as-is | Chair reviews for approval |
| Edited | Output edited and accepted | Chair reviews for approval |
| Rejected | Output could not be reconciled | Secretariat drafts manually |
| RequiresReRun | Output quality insufficient | Re-run with adjusted prompt or data |

### AI Builder Must Never

- Approve or publish minutes directly
- Mark decisions as official
- Create Planner tasks from unapproved output
- Send Teams notifications from draft content
- Bypass the human review gate

## Future Graph Path

When Microsoft Graph transcript retrieval is enabled via `decisions_EnableGraphTranscriptSync`:

1. A scheduled or event-driven flow checks for Teams meeting recordings with transcript availability.
2. Transcript text is retrieved via Graph API and stored as `dft_Transcript` with `dft_Source = 'Graph'`.
3. The existing AI Builder pipeline processes the Graph-sourced transcript identically to manually entered transcripts.
4. The human review gate still applies — Graph source does not bypass review.

## AI Builder Prompt Governance

See `docs/architecture/ai-builder-minutes-pattern.md` for:
- Prompt naming and versioning convention
- Prompt approval workflow in `dft_AIBuilderPrompt`
- Audit record schema in `dft_AIBuilderRun`
- Safety defaults and feature flag guidance

## Related

- `powerplatform/src/flows/ai-transcript-minutes.yaml` — flow contract for the AI Builder transcript processing pipeline
- `docs/architecture/ai-builder-minutes-pattern.md` — AI Builder prompt governance and safety defaults
- `docs/architecture/ai-human-review-gates.md` — detailed human review gate model and evidence requirements
- `powerplatform/src/flows/teams-summary-post.yaml` — downstream flow gated on human review
- `powerplatform/src/flows/planner-sync.yaml` — downstream flow gated on human review
