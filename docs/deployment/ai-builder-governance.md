# AI Builder Governance

This document defines the governance model for AI Builder and Copilot prompt usage in the CareOps Decisions workspace.

## Scope

Covers AI Builder prompts used for transcript-to-minutes workflows, decision extraction, and action item generation. This is not a general AI governance policy — it applies only to the Decisions workspace AI surfaces.

## Approved Prompt Records

Every AI Builder prompt used in the solution must be registered:

| Field | Description |
| --- | --- |
| Prompt Name | Unique logical name in the AI Builder prompt catalogue |
| Purpose | What the prompt does (summarisation, extraction, formatting) |
| Input Schema | Expected JSON structure for the prompt input |
| Output Schema | Expected JSON structure for the prompt output |
| Max Tokens | Token limit configured in the prompt |
| Temperature | Temperature setting (default 0.3 for extraction, 0.7 for summarisation) |
| Version | Prompt version string, incremented on each edit |

## Human Review States

AI-generated output must pass through a human review gate before publication. The following states apply:

| State | Meaning | Next Action |
| --- | --- | --- |
| `draft` | AI output generated, not yet reviewed | Human review required |
| `in-review` | Assigned to a reviewer | Reviewer accepts or rejects |
| `accepted` | Reviewer approved as-is | Output can be published |
| `edited` | Reviewer modified output before approval | Edited version is published; original AI output retained in audit log |
| `rejected` | Reviewer rejected the output | Returned to draft with reviewer notes |

## Audit Fields

Each AI prompt run must record:

- Prompt name and version
- Timestamp of execution
- Request correlation ID
- Input size (tokens or characters)
- Output size (tokens or characters)
- Duration (milliseconds)
- Result (success / partial / failure)
- Error detail (if any)
- Human review decision (if applicable)
- Reviewer identity and timestamp
- Published flag

## Test Fixtures

Test data should include:

- A successful AI transcript summarization with known expected output
- A partial failure scenario (e.g. truncated output)
- A human review queue record awaiting action
- An accepted review record with reviewer metadata
- A rejected review record with reviewer notes

## Gates

- No AI Builder prompt is called without an active feature-switch flag
- AI-generated minutes cannot be published without a human review decision
- Prompt version changes require re-validation of output schemas
- AI Builder capacity and licensing must be confirmed before any live prompt execution
