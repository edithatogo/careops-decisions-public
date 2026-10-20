# ADR 0004: AI Human Review Gate

## Status

Accepted.

## Context

Transcript-to-minutes generation can improve productivity, but meeting minutes, decisions, actions, and records are governance artifacts. AI output must not become official without review.

## Decision

AI Builder or Copilot-generated content remains draft until a human reviews, edits, and approves it.

## Consequences

- AI prompt versions, input classification, output schema, run status, reviewer, and publication state must be stored.
- Tests must cover draft status, review, rejection, approval, and audit trail.
- Official minutes and decision records must not be automatically published from raw AI output.
