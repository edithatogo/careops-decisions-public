# Per-Meeting Feature Switchboard

## Purpose

The per-meeting feature switchboard is the control layer that decides which optional meeting capabilities are active for a given meeting instance. It keeps meeting behavior configurable without changing the core meaning of the meeting record itself.

The switchboard is intended for operational choices such as invite handling, agenda publishing, paper registration, minutes review, transcript assistance, action sync, reporting exposure, and Teams or Planner side effects.

Feature definitions describe each optional capability, and overrides narrow those definitions at more specific levels.

## Inheritance Model

The effective switchboard should be resolved from the most general setting to the most specific setting:

1. Repository or product baseline defaults.
2. Environment profile defaults, such as local dev, shared test, or target deployment.
3. Governance body or meeting series defaults.
4. Meeting-level overrides.
5. Runtime connector availability and security checks.

Lower levels can narrow or disable capabilities, but they cannot override a higher-level block. A meeting override can turn a capability off for one meeting, but it cannot enable a connector that the target environment does not expose or a role that the user cannot hold.

The meeting series is the natural inheritance point for repeated meetings. New meetings should inherit the series baseline, then allow controlled per-meeting overrides for one-off exceptions.

## Runtime Behavior

- The app should read the resolved switchboard before showing actions or editable surfaces.
- Flows should read the same resolved state before invoking external connectors.
- Disabled capabilities should be treated as skipped or suppressed, not as failures.
- The meeting record should retain the effective decision so later review can explain why a feature was or was not used.
- Any manual override should be visible as a deliberate exception, not hidden in form defaults.

## Governance, Reporting, And Audit

The switchboard has governance impact because it changes how a meeting is managed, published, and reported. It should therefore be visible to secretariat, chair, and audit workflows.

- Reporting should distinguish between feature policy, effective feature state, and actual execution outcome.
- Audit trails should show who changed a switchboard setting, when it changed, what inherited value was replaced, and which downstream connector or workflow was affected.
- Governance reporting should be able to answer whether a meeting was configured for draft-only handling, AI-assisted minutes, Teams posting, Planner sync, SharePoint registration, or other optional paths.
- The switchboard must not weaken access control or records classification. Security, retention, and classification still come from the underlying governance model.
- Operational reports should surface mismatches between intended configuration and actual environment availability, especially for connectors that are enabled in design but unavailable in the target tenant.

## Environment-Gated Connectors

The switchboard needs an explicit environment gate for connector-backed work. The local build can document or simulate the paths, but production execution should only occur when the target environment has the required connection references, permissions, and environment variables.

The environment-gated connectors in this repo are primarily:

- Teams
- Planner
- SharePoint
- Microsoft Graph
- Office 365 Outlook
- Office 365 Users
- AI Builder, where the prompt and review gate are present

The rule is simple: the switchboard can request a capability, but the environment decides whether that capability can actually run. Missing connector references should produce a clear disabled or pending state, not a silent fallback to uncontrolled behavior.

## Implementation Status

Track 22 documents the per-meeting feature switchboard as part of the MVP activation and enhancement phase. The current local build remains Dataverse-first and feature-complete before live deployment, while Teams, Planner, SharePoint, and Graph wiring stay environment-gated.

The implementation plan is to keep the switchboard logic aligned with the meeting series and meeting configuration records, then expose the resolved state through validation, reporting, and operational status docs before live connector activation is attempted.
