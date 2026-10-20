# AI Human Review Gates

## Purpose

AI Builder and Copilot-style assistance can accelerate minutes, decision extraction, action extraction, and unresolved-item capture. It must not become an unattended publishing path. Decisions Workspace treats generated text as draft evidence until an accountable reviewer approves it.

## Gate Model

| Gate | Applies to | Required outcome |
| --- | --- | --- |
| Input classification | Transcript, notes, papers | Confirm the source classification is permitted for the configured prompt. |
| Prompt approval | AI Builder prompt | Use only approved prompt names and versions recorded in Dataverse. |
| Run logging | AI Builder execution | Record prompt name, version, input hash, output hash, model/provider, timestamp, requester, and source record. |
| Draft creation | Minutes, decisions, actions | Create draft records only. Do not publish, close, or notify from AI output. |
| Human review | Draft output | Reviewer must accept, edit, or reject every generated minutes section, decision, and action. |
| Publication approval | Minutes and registers | Approved content can be published only after reviewer, approval time, and source run are recorded. |

## Required Dataverse Evidence

- `dft_AIBuilderPrompt`: prompt purpose, approved version, classification allowance, owner, approval status, and effective dates.
- `dft_AIBuilderRun`: source transcript, meeting, prompt, input hash, output hash, run status, human review status, reviewer, reviewed on, and exception notes.
- `dft_Minutes`: AI source run, draft status, reviewer, approval state, publication state, and source transcript.
- `dft_Decision` and `dft_ActionItem`: AI source run and draft/review status when generated from transcript output.
- `dft_IntegrationLog`: skipped, failed, and completed AI processing events with correlation ID.

## Reviewer Responsibilities

- Check that generated minutes reflect the meeting record, not only the transcript wording.
- Remove hallucinated attendees, decisions, actions, dates, owners, and approvals.
- Confirm confidential or restricted content remains in the correct record, not in Teams posts or broad summaries.
- Reject the run when the prompt output cannot be reconciled to the source material.
- Record edits and review outcome before approving minutes or publishing register entries.

## Implementation Controls

- The AI flow must be feature-flagged by `decisions_EnableAiBuilderMinutes`.
- The active prompt must be resolved from `decisions_AiBuilderPromptName` and matched to an approved `dft_AIBuilderPrompt` row.
- The flow must stop before connector invocation when classification, prompt approval, or source transcript requirements fail.
- Output records must start in a draft or pending-review state.
- Publishing and Teams posting flows must check human-review status before sending summaries.
- Repeat processing must use transcript ID, prompt name/version, input hash, and existing run ID as the replay guard.

## Acceptance Criteria

- AI disabled path logs a skipped integration record and creates no draft minutes.
- AI enabled path creates one run row and draft records from a seeded transcript.
- Re-running the same transcript and prompt does not duplicate draft records.
- A reviewer can approve, edit, or reject output and the outcome is queryable for audit.
- No AI-generated content can reach published minutes, Teams posts, or decision/action registers without human review evidence.
