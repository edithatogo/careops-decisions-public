# Release Artifacts

Use `scripts/New-ReleaseArtifact.ps1` to build the Power Platform release packages from the unpacked solution source in `powerplatform/solutions/DecisionsWorkspace/src`.

The script writes artifacts under `artifacts/solutions/DecisionsWorkspace/` using deterministic paths:

- `artifacts/solutions/DecisionsWorkspace/managed/DecisionsWorkspace_managed.zip`
- `artifacts/solutions/DecisionsWorkspace/unmanaged/DecisionsWorkspace_unmanaged.zip`

## Default Behavior

If you do not pass package-type switches, the script builds both managed and unmanaged artifacts.

It validates that:

- `pac` is available on `PATH`
- the unpacked solution source folder exists

## Dry Run

Use `-DryRun` to inspect the exact PAC commands and output paths without creating files:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-ReleaseArtifact.ps1 -DryRun
```

## Build Commands

Build both release artifacts:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-ReleaseArtifact.ps1
```

Build only the managed package:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-ReleaseArtifact.ps1 -Managed
```

Build only the unmanaged package:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-ReleaseArtifact.ps1 -Unmanaged
```

Round-trip the unmanaged package into a throwaway folder:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Unpack-Solution.ps1 -SolutionZip artifacts/solutions/DecisionsWorkspace/unmanaged/DecisionsWorkspace_unmanaged.zip -SolutionSourcePath artifacts/roundtrip/DecisionsWorkspace
```

Preview the import command without touching a target environment:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-Solution.ps1 -SolutionZip artifacts/solutions/DecisionsWorkspace/unmanaged/DecisionsWorkspace_unmanaged.zip -DryRun
```

## Release Notes Template

Use the repo version from `VERSION` for the product release label and the 4-part solution version only when the packaging tool or release artifact demands it. Do not mix the two in the same file.

Include a `RELEASE_NOTES.md` in every release artifact. Use the following template:

```markdown
# Release {Version}

**Date:** {YYYY-MM-DD}
**Environment:** {development | test | enterprise}
**Solution:** CareOps Decisions (DecisionsWorkspace) {Managed | Unmanaged}
**Operational Owner:** {Named owner responsible for deployment monitoring and support}

## Summary

{One-paragraph summary of what this release contains}

## New Features

- {Feature description} ({track reference})

## Fixed Issues

- {Issue description} ({issue reference})

## Changes

- {Change description} ({source commit or artifact})

## Deployment Profile

- **Profile:** {local | development | test | enterprise}
- **Package Type:** {Managed | Unmanaged}
- **Solution Checker:** {Passed | Not Run | Not Applicable}
- **Validation Status:** {CI validate.yml passed | Manual check only}

## Known Issues

- {Issue description, workaround if available}

## Rollback Notes

- **Rollback method:** Import previous version or restore from backup
- **Rollback risk:** {Low | Medium | High}
- **Data impact:** {Describe any data that could be lost on rollback}
- **Rollback tested:** {Yes | No | Not applicable}
```

## Rollback Documentation

Every release must identify the rollback strategy before deployment:

| Strategy | When to Use | Commands |
| --- | --- | --- |
| Overwrite import | Most unmanaged updates | `pac solution import --path previous_version.zip --publish-changes` |
| Restore from backup | Breaking schema changes | Restore environment from backup (requires admin access) |
| Layer-add managed | Managed solution roll-forward | Import previous managed version over current |

Rollback prerequisites:
- Previous solution zip is archived and accessible
- Environment backup exists if schema changes are irreversible
- Rollback plan is reviewed before deployment

## Notes

- This script packages the repo-local solution sources only.
- It does not export from a live environment.
- Use the existing `scripts/Pack-Solution.ps1`, `scripts/Export-Solution.ps1`, and `scripts/Unpack-Solution.ps1` wrappers when you need the simpler one-step PAC operations around the same solution.
