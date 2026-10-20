# Power Platform Settings

This folder contains deployment settings and ALM manifests for CareOps Decisions.

- `deployment-settings.sample.json`: sample values for solution import.
- `environment-variables.md`: human-readable environment variable notes and safe example values.
- `alm-profiles.json`: local, development, test, and future enterprise ALM profiles.
- `connection-references.json`: connector ownership and reference manifest.
- `environment-variable-manifest.json`: canonical environment variable manifest.
- `scripts/New-EnvironmentConfigurationChecklist.ps1`: generator for the environment configuration wizard.
- `scripts/New-LocalDeploymentSettings.ps1`: generates ignored local deployment settings from live Dylan connections.
- `docs/deployment/environment-configuration-wizard.md`: repo-local readiness checklist for environment variables, connection references, Teams, Planner, SharePoint, and AI Builder.

Use `scripts\Test-AlmActivation.ps1` to validate these artifacts before live Power Platform work.

The sample deployment file keeps documented examples for Planner, SharePoint, and AI Builder. Treat those as placeholders unless you are explicitly targeting the documented example environment.

Do not commit secrets, connection IDs for real users, or tenant-specific credentials. `powerplatform/settings/deployment-settings.local.json` is ignored and is the intended place for live Dylan connection IDs.
