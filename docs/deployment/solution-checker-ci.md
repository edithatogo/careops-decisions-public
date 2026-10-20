# Solution Checker CI

This repository now has two GitHub Actions entry points for solution packaging and static checks:

- `.github/workflows/validate.yml` runs repository validation on every PR and main-branch push, then packs the unmanaged solution into `artifacts/solutions/DecisionsWorkspace_unmanaged.zip`.
- `.github/workflows/release-artifact.yml` packages the same unmanaged solution for release runs and manual dispatches.

The Power Platform solution checker is enabled only when the repository is configured for it. That keeps PR validation usable before live environment credentials exist.

## Required GitHub Configuration

Set these repository values before enabling the checker:

- Repository variable `SOLUTION_CHECKER_ENABLED=true`
- Repository variable `POWER_PLATFORM_ENVIRONMENT_URL`
- Repository secret `POWER_PLATFORM_APP_ID`
- Repository secret `POWER_PLATFORM_CLIENT_SECRET`
- Repository secret `POWER_PLATFORM_TENANT_ID`
- Optional repository variable `SOLUTION_CHECK_GEO` if you need a non-default checker region

## What The Workflows Do

Both workflows:

1. Check out the repo.
2. Install Power Platform CLI tooling with `microsoft/powerplatform-actions/actions-install@v1`.
3. Pack `powerplatform/solutions/DecisionsWorkspace/src` into an unmanaged solution zip.
4. Upload the packed zip as a workflow artifact.
5. Run `microsoft/powerplatform-actions/check-solution@v1` when checker configuration is enabled.

The checker action uploads its own logs artifact using the name `CheckSolutionLogs`.

## Local Equivalent

For local runs, use the wrapper script directly:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/Invoke-SolutionChecker.ps1 `
  -SolutionZip artifacts/solutions/DecisionsWorkspace_unmanaged.zip
```

## Notes

- The validation workflow does not require checker secrets for PRs.
- The release workflow is useful for manual packaging or release-published runs once the checker configuration exists.
- If you need to inspect the packed solution zip locally, the CI artifact path is the same one used by the workflows.
