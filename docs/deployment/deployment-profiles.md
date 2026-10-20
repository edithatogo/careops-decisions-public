# Deployment Profiles

This document describes the four deployment profiles maintained in `powerplatform/settings/alm-profiles.json`. Each profile corresponds to a stage in the release pipeline.

## Profile Definitions

| Profile | Environment | Managed | Allow Live Writes | Purpose |
| --- | --- | --- | --- | --- |
| `local` | None (repo-only) | No | No | Repo validation without live Power Platform writes |
| `development` | Dylan (dev) | No | Yes | Unmanaged development in the Dylan environment |
| `test` | Placeholder | No | No (gated) | Shared test import after development validation |
| `enterprise` | Placeholder | Yes | No (gated) | Future managed deployment with Azure/Entra |

## Profile Details

### Local Profile

Used for CI and local repo validation. No live environment is required.

```powershell
# Run validation checks
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Invoke-RepoValidation.ps1 -RequireImplementationScaffolds

# Pack solution (dry-run safe)
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-ReleaseArtifact.ps1 -Unmanaged -DryRun
```

The local profile does not require environment authentication. All checks operate on source artifacts.

### Development Profile

Targets the Dylan Power Platform development environment. This is the primary active live-write profile.

**Prerequisites:**
- Authenticated PAC session (`pac auth list` shows the Dylan environment)
- Generated local settings file (`powerplatform/settings/deployment-settings.local.json`)
- No placeholder values in local settings

**Workflow:**
```powershell
# 1. Confirm environment readiness
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Get-EnvironmentReadiness.ps1

# 2. Generate local settings
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-LocalDeploymentSettings.ps1

# 3. Import solution
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-Solution.ps1 `
  -TargetProfileName development `
  -SettingsFile powerplatform/settings/deployment-settings.local.json `
  -PublishChanges

# 4. Capture deployment evidence
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-DylanDeploymentEvidence.ps1
```

### Test Profile

A shared test environment for integration validation. Currently gated until:
- The test environment URL is provisioned and configured
- Connection references are resolved for the test environment
- Rollback documentation exists for the test environment URL

```powershell
# When ready, import using the test profile
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-Solution.ps1 `
  -TargetProfileName test `
  -SettingsFile powerplatform/settings/deployment-settings.test.json `
  -PublishChanges
```

### Enterprise Profile

Future managed deployment with Azure, Entra, Graph, and Power BI/Fabric readiness. This profile is design-only until tenant access and approvals exist.

**Design assumptions:**
- Managed solution packaging (`managed: true`)
- Service principal authentication (non-interactive)
- Environment URL known after enterprise provisioning
- DLP policies, sensitivity labels, and audit logging in place
- Release notes and rollback plan required before first deployment

## Switching Profiles

The profile is selected via `-TargetProfileName` on import scripts or by editing `powerplatform/settings/alm-profiles.json`. The `Resolve-AlmProfile.ps1` helper reads the catalog and validates the profile exists before any live operation.

## Profile Validation

Run the following to confirm the active profile is configured correctly:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-AlmActivation.ps1
```

This validates solution metadata, version format, and release checklist coverage against the selected profile.

## Versioning Model

The repository uses two related version formats:

- `VERSION` in the repo root is the canonical product version and uses core SemVer, for example `1.0.0`.
- Solution artifacts and generated release notes can use a four-part Power Platform version, for example `1.0.0.0` or `1.0.0.1`.

That split is intentional. Repository metadata, changelog entries, and quick-start docs should follow the repo version. Solution import/export and release-note tooling follow the Power Platform artifact versioning model.
