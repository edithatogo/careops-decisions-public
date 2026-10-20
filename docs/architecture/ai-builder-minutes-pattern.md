# AI Builder Minutes Pattern

## Purpose

AI Builder can speed up minute drafting, decision extraction, action extraction, and unresolved-item capture. It is not an approval authority.

The AI Human Review Gates document (ai-human-review-gates.md) defines the gate model, Dataverse evidence tables, reviewer responsibilities, and implementation controls in detail.

## Inputs

Use bounded, reviewable inputs:

- Meeting record.
- Agenda items and decision sought fields.
- Attendance, apologies, quorum, and conflicts.
- Paper register with SharePoint links, not uncontrolled file dumps.
- Transcript or meeting notes.
- Previous actions and open decisions for follow-up context.

Do not include unrelated mailbox content, unapproved personal information, or patient information unless a formal privacy and governance basis exists.

## Draft Outputs

The prompt should return structured draft content:

- Minute sections by agenda item.
- Candidate decisions with source agenda item and proposed status.
- Candidate actions with owner, due date, and source agenda item.
- Risks, deferrals, unresolved questions, and missing-information warnings.
- Review checklist for secretariat and chair.

## Human Review Gate

AI output remains draft until reviewed.

1. Secretariat reviews the draft against agenda, papers, transcript, and attendance.
2. Secretariat edits minutes, decisions, and actions in Dataverse.
3. Chair reviews and approves the official record.
4. Approved minutes can be published to SharePoint or Teams only if the meeting switchboard allows it and the relevant connector is available.
5. Planner tasks can be created only from approved action records when Planner sync is enabled for that meeting.
6. If AI Builder, SharePoint, Teams, or Planner are missing or disabled, the flow must stop at the draft or skipped state and write an integration log entry.
7. AI Builder cannot approve minutes, mark decisions as official, publish content, or create Planner tasks. All of those actions require a permitted human role.

## Audit Record

Each AI run should store:

- Prompt name and version.
- Source meeting, transcript, agenda, and minute records.
- Requester.
- Input hash or source reference.
- Output hash or generated artifact reference.
- Model/provider.
- Start and completion time.
- Status, error message, and retry count.
- Human reviewer, review timestamp, and review outcome.

These fields map to the `dft_AIBuilderRun` table. Related audit evidence is held in `dft_AIBuilderPrompt` (prompt approval), `dft_Minutes` (minute drafts), `dft_Decision` and `dft_ActionItem` (candidate records), and `dft_IntegrationLog` (processing events).

## Prompt Naming and Versioning

AI Builder prompts must follow a consistent naming and versioning convention:

- **Prompt name format**: `Decisions - {Feature} - {Purpose}`.
- **Version format**: Semantic `{major}.{minor}`, incremented when the prompt contract or output shape changes.
- **Approved prompts**: Recorded in `dft_AIBuilderPrompt` with effective start and end dates, approval status, and classification allowance.
- **Active prompt resolution**: The flow must resolve the prompt from the `decisions_AiBuilderPromptName` environment variable and match it to an approved `dft_AIBuilderPrompt` row before execution.
- **Prompt retirement**: Superseded versions must remain in the audit trail; only one version should be active at a time.

## Prompt Contract

The prompt should ask for JSON-shaped output that maps cleanly to Dataverse records. Free text can be generated for minute paragraphs, but decisions and actions should be structured.

Required top-level fields:

- `summary`
- `agendaMinutes`
- `candidateDecisions`
- `candidateActions`
- `risks`
- `deferredItems`
- `missingEvidence`
- `reviewChecklist`

The flow should reject output that cannot be parsed or that omits required review fields.

## Safety Defaults

- `decisions_EnableAiBuilderMinutes` stays false until licensing, capacity, and review governance are confirmed.
- No AI-generated content is official until approved by a permitted human role.
- AI output should be labelled as draft in the app and reports.
- Integration logs should record skipped runs when the feature is disabled.
- SharePoint, Teams, and Planner side effects remain deferred until human review and the meeting-level feature switchboard both permit them.
