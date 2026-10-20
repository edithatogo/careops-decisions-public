# ADR 0007: Feature Switchboard Pattern

## Status

Accepted.

## Context

Governance meetings have varying requirements for capabilities such as
voting, AI minute drafting, Planner task sync, Teams post notifications,
SharePoint document publishing, Outlook invite sync, and org hierarchy
lookup. A single meeting series may enable voting but disable AI minutes;
a different body may enable both. The app needs a resolution order for
feature states that works across environment defaults, governance body
preferences, meeting series defaults, and per-meeting overrides.

## Decision

Implement a feature switchboard using three Dataverse tables in a chain:

1. `dft_FeatureDefinition` — environment-level default state for each
   capability.
2. `dft_FeatureTemplate` — governance-body or meeting-series-scoped
   override of the definition default.
3. `dft_MeetingFeatureOverride` — per-meeting override of the inherited
   template or definition default.

Resolution order: meeting override > meeting series template > governance
body template > feature definition default.

## Consequences

- Feature resolution is data-driven and does not require solution changes
  to enable or disable a capability for a specific body or meeting.
- The app must implement a resolution function that walks the inheritance
  chain at runtime.
- Each meeting feature override is auditable with a reason field.
- Environment-variable manifest does not need per-capability switches;
  only the Dataverse table seed data determines defaults.
- Feature switchboard auditing is part of the governance-configuration-health
  dashboard.
