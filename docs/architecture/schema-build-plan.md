# Schema Build Plan

This repo keeps Dataverse schema intent in source control and exports a deterministic build plan alongside the YAML sources.

## Source Of Truth

- `powerplatform/src/schema/dataverse-tables.yaml`
- `powerplatform/src/schema/choices.yaml`
- `powerplatform/src/schema/relationships.yaml`

## Exported Plan

- `powerplatform/src/schema/schema-build-plan.json`

The exported plan is intentionally small and explicit. It captures the order needed to create tables, the primary field for each table, the choice sets each table depends on, and the relationship phases that should be applied after tables exist.

## Build Phases

1. Create tables in dependency order.
2. Apply `mvp` relationships first.
3. Apply `future` relationships after the MVP model is stable.

## Why This Shape

The schema YAML is readable by humans, but the build process needs a stable, deterministic artifact. Rather than depend on an external YAML parser, the exporter does conservative text extraction and validates it against the committed JSON plan.

If the YAML ever drifts beyond that conservative parser, the JSON plan becomes the handoff contract for the next build step. The handoff stays explicit and reviewable, and the schema source files remain the long-term design reference.
