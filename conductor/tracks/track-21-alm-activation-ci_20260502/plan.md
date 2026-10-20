# Track 21 Plan

1. Add GitHub Actions workflow for repository validation.
2. Add `Invoke-SolutionChecker.ps1` for explicit solution checker runs.
3. Add ALM profile and manifest files under `powerplatform/settings`.
4. Add `Test-AlmActivation.ps1` and wire it into repo validation.
5. Add live ALM activation handoff documentation.

## Validation

- `scripts\Test-AlmActivation.ps1`
- `scripts\Invoke-RepoValidation.ps1 -RequireImplementationScaffolds`
