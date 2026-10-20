# ADR 0006: Power BI Deferred Until Stable Dataverse Data

## Status

Accepted.

## Context

Governance reporting is important, but Power BI/Fabric reports are only useful after Dataverse tables, relationships, security, and sample data are stable.

## Decision

Start reporting with Dataverse views and app dashboards. Defer Power BI/Fabric implementation until the Dataverse model and MVP workflows are stable.

## Consequences

- Metrics and semantic model boundaries should be documented now.
- Power BI security must align with Dataverse roles and committee membership later.
- Power BI/Fabric must not block MVP release.
