# Reporting Artifacts

This folder describes the Dataverse-first reporting surfaces for the MVP. These are implementation contracts for views, charts, and app dashboards before any Power BI or Fabric model is introduced.

## MVP Reporting Order

1. Dataverse operational views for secretary, chair, member, and auditor workflows.
2. Model-driven or canvas dashboard surfaces that reuse the same filtered views.
3. Smoke-test evidence that each dashboard can be populated from seeded data.
4. Power BI/Fabric semantic model only after the Dataverse model, access model, and sample data are stable.

## Files

- `dataverse-dashboards.yaml`: dashboard, view, metric, and data-quality contract for the MVP reporting layer.

## MVP Dashboard Coverage

- Meeting pipeline: next-30-day draft, planned, or invited meetings, including meetings missing agenda items.
- Minutes review and review gates: draft, pending-review, chair-approval, and AI runs still awaiting human review.
- Decision register: open decisions, assigned decisions, and decisions overdue for review.
- Actions: open, overdue, owned-by-me, and Planner sync exceptions.
- Governance configuration: hierarchy coverage, feature-switch defaults, contextual templates, and meeting overrides.
- Access exceptions: active restricted, exception, or break-glass access rules.
- Connector health: connector and flow activity, failed runs, retried runs, and stale queued integration log rows.
- Power BI/Fabric stays future-facing until the Dataverse contract and access model are stable.

## Data-Quality Checks

- Missing owners on governance bodies, decisions, action items, and papers.
- Overdue decision or terms-of-reference reviews.
- Stale integration statuses older than 24 hours.
- Unapproved minutes with a populated publish URL.
- Actions marked as Planner sync succeeded without a Planner task ID.

## Access Principles

- Record-level access follows Dataverse roles and governance body membership.
- Restricted minutes, papers, transcripts, and AI outputs must not be surfaced in broad dashboards.
- Aggregates can be broader than record access only when counts cannot identify restricted records.
- Power BI and Fabric stay deferred for the local build; do not create report dependencies that block app-core delivery.
