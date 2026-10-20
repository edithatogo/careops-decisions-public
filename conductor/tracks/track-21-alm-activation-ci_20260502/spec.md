# Track 21: ALM Activation And CI

## Goal

Implement the repo-side ALM activation path needed before creating or exporting the real unmanaged Power Platform solution.

## Scope

- CI workflow for repository validation.
- Solution checker wrapper.
- Deployment profile manifest.
- Connection reference and environment variable manifests.
- ALM activation validation.
- Live-environment handoff checklist for creating/exporting the real unmanaged solution.

## Acceptance Criteria

- CI can run the same validation command as local development.
- ALM manifests are present and validated.
- Live solution activation is gated on environment URL, publisher, connection references, and deployment settings.
- No script performs live Power Platform writes without explicit parameters and confirmation.
