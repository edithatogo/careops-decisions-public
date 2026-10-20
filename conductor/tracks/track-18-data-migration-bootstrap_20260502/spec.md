# Track 18: Data Migration And Environment Bootstrap

## Goal

Prepare repeatable seed, import, migration, and environment bootstrap paths before live NSW Health data is loaded.

## Scope

- Fixture packs: small (single committee), medium (multi-level hierarchy with multiple meetings), and edge-case (retired bodies, superseded ToR, cancelled meetings, complex alternate/proxy patterns) JSON/placeholder datasets.
- Import mapping templates: source-to-target field mapping specifications for committee registers, minutes registers, paper registers, decision logs, and action logs.
- Bootstrap runbook: step-by-step process for creating a clean Dataverse development environment with schema build, seed import, and validation.
- Schema migration policy: rules for schema changes after data exists, with destructive-change review checklist and safe-add vs safe-rename vs breaking-change categories.

## Acceptance Criteria

- Fixture packs are documented with record counts and can be referenced by import scripts.
- Import mappings reference known Dataverse table and column names with source field examples.
- Bootstrap runbook documents the environment creation sequence.
- Schema migration policy prevents destructive schema changes without explicit review.

## Out of Scope

- Live data migration from NSW Health source systems (gated on Outlook/SharePoint/Teams access).
- Automated migration tooling (the policy and template layer comes first; tooling is future).
- Production data seeding (all sample data remains explicitly synthetic).
