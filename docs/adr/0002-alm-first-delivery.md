# ADR 0002: ALM-First Delivery

## Status

Accepted.

## Context

The solution includes Dataverse schema, app surfaces, flows, environment variables, connection references, roles, tests, and future Azure/Entra integration. Manual maker-only changes would be difficult to reconcile and promote.

## Decision

Use a repo-first ALM approach. Source-controlled artifacts, validation scripts, deployment settings, and release checklists are the implementation contract.

## Consequences

- Live environment work should be exported or represented back in source.
- `pac` and `pacx` are the primary CLIs for implementation.
- Release readiness requires validation output, versioning, target environment, evidence, and rollback notes.
