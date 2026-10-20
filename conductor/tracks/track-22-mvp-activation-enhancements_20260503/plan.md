# Track 22 Plan

1. Activation baseline
   - Confirm the v1.0.0 Dylan Dataverse export and define the app-core-before-deployment boundary.
   - Map the app and flow surfaces that need to be activated from source.
   - Capture the local feature-complete scope versus the live connector and environment gates.

2. Release artifacts and solution checker
   - Add release package outputs, release notes, and handoff evidence.
   - Wire solution checker and validation gates into the activation path.

3. Sample data and configuration
   - Add importable sample data and reset/reseed steps for the development environment.
   - Provide a configuration wizard or scripted equivalent for environment-specific values.
   - Define the per-meeting feature switchboard inheritance chain from environment defaults to meeting-series defaults to meeting overrides, and keep connector enablement explicit.
   - Treat SharePoint document libraries, Teams file locations, document registration, and Word/PDF export as DMS candidates for the later deployment layer.

4. Human review gates
   - Define review states and publish blocks for draft and AI-assisted content.
   - Record the approval checkpoints and audit fields needed for traceability.

5. Reporting
   - Add reporting source artifacts and metric definitions for operational and governance use.
   - Keep reporting scoped to the activated MVP rather than future Azure/Fabric expansion.
   - Make the local Dataverse views and metrics complete before depending on Teams, Planner, or Graph access.
   - Ensure reporting can distinguish inherited defaults, meeting overrides, and actual connector execution states for the switchboard.

6. Tests and validation
   - Expand smoke tests and automated checks for app and flow activation, sample import, configuration, review gates, and reporting.
   - Keep the activation path aligned with repo validation and solution checker output.
   - Separate tests that pass locally from tests that require live Teams, Planner, SharePoint, or Graph access.

7. Worker split
   - gpt-5.4-mini medium worker 1: activation baseline and app-core boundary.
   - gpt-5.4-mini medium worker 2: release artifacts and solution checker.
   - gpt-5.4-mini medium worker 3: sample data, reset/reseed, and configuration wizard.
   - gpt-5.4-mini medium worker 4: human review gates and approval logging.
   - gpt-5.4-mini medium worker 5: reporting source artifacts, Dataverse metrics, and switchboard visibility.
   - gpt-5.4-mini medium worker 6: tests, smoke coverage, and validation gates.

8. Local implementation hardening wave
   - Worker A: seed payload validation and deterministic import contract hardening.
   - Worker B: app contract validation for screen/control/source expectations.
   - Worker C: flow contract validation for triggers, actions, environment variables, and connector references.
   - Worker D: reporting, switchboard traceability, and data-quality checks for local Dataverse-first metrics.
   - Worker E: operations readiness, runbook, and support evidence hardening.
   - Worker F: Conductor status, track documentation, and implementation status alignment.
   - Record pushed commits `8509078` and `185ac9c`; CI passed for `185ac9c`.
   - Keep local build completeness separate from live Teams, SharePoint, Planner, and Graph deployment blockers.

## Validation

- `scripts\Test-PlanningArtifacts.ps1`
- `scripts\Invoke-RepoValidation.ps1 -RequireImplementationScaffolds`
- `scripts\Test-AlmActivation.ps1`
- `scripts\Test-ImplementationReadiness.ps1 -Strict`
- `conductor/tracks/track-22-mvp-activation-enhancements_20260503/metadata.json` parses as valid JSON.
