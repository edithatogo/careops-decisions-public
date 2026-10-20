# PAC / PACX CLI Reference

This document catalogs the `pac` and `pacx` commands used in this repository for Power Platform ALM. Commands are grouped by workflow stage.

## Authentication

```powershell
# List active auth profiles
pac auth list

# Select an existing profile by index
pac auth select --index 0

# Create a new auth profile (interactive or device)
pac auth create --environment https://environment.example.invalid/

# Create a non-interactive service-principal auth (future use)
pac auth create --environment https://environment.example.invalid/ `
  --applicationId <app-id> --clientSecret <secret> --tenant <tenant-id>
```

## Solution Commands

### Export

```powershell
# Export unmanaged solution from the active environment
pac solution export `
  --name DecisionsWorkspace `
  --path powerplatform/solutions/DecisionsWorkspace.zip `
  --managed false

# Export managed solution
pac solution export `
  --name DecisionsWorkspace `
  --path powerplatform/solutions/DecisionsWorkspace_managed.zip `
  --managed
```

### Unpack

```powershell
# Unpack an exported solution zip into source folder
pac solution unpack `
  --zipfile powerplatform/solutions/DecisionsWorkspace.zip `
  --folder powerplatform/solutions/DecisionsWorkspace `
  --packagetype Unmanaged

# Unpack managed to inspect but not modify
pac solution unpack `
  --zipfile powerplatform/solutions/DecisionsWorkspace_managed.zip `
  --folder artifacts/roundtrip/DecisionsWorkspace_managed `
  --packagetype Managed
```

### Pack

```powershell
# Pack source folder back into a solution zip
pac solution pack `
  --folder powerplatform/solutions/DecisionsWorkspace `
  --zipfile artifacts/solutions/DecisionsWorkspace_unmanaged.zip `
  --packagetype Unmanaged

# Pack as managed
pac solution pack `
  --folder powerplatform/solutions/DecisionsWorkspace `
  --zipfile artifacts/solutions/DecisionsWorkspace_managed.zip `
  --packagetype Managed
```

### Import

```powershell
# Dry-run import (no publish, validates only with settings)
pac solution import `
  --path artifacts/solutions/DecisionsWorkspace_unmanaged.zip `
  --settings-file powerplatform/settings/deployment-settings.sample.json

# Live import with publish
pac solution import `
  --path artifacts/solutions/DecisionsWorkspace_unmanaged.zip `
  --settings-file powerplatform/settings/deployment-settings.local.json `
  --publish-changes

# Import without settings
pac solution import `
  --path artifacts/solutions/DecisionsWorkspace_unmanaged.zip `
  --publish-changes
```

### Solution Checker

```powershell
# Run solution checker on a packed zip
pac solution check `
  --path artifacts/solutions/DecisionsWorkspace_unmanaged.zip `
  --geo UnitedStates

# Run checker with custom output directory
pac solution check `
  --path artifacts/solutions/DecisionsWorkspace_unmanaged.zip `
  --outputDirectory artifacts/solution-checker-results `
  --geo UnitedStates
```

### Solution Version

```powershell
# Update solution version before packing
pac solution version-strategy `
  --solutionName DecisionsWorkspace `
  --strategy useSolution

# Set a specific version
pac solution version-strategy `
  --solutionName DecisionsWorkspace `
  --strategy manual --version 1.0.0.1
```

## Environment and Connection Commands

### Environment List

```powershell
# List all environments the authenticated user can access
pac admin list

# Show current environment
pac org who
```

### Connection Reference and Environment Variable Management

```powershell
# List connection references in the current environment
pac connector list

# List environment variables
pac env-variable list

# Download current solution settings as a JSON file
pac solution online-settings-retrieve `
  --solution-name DecisionsWorkspace `
  --output-file powerplatform/settings/current-settings.json
```

## PACX Commands (Power Platform CLI Extended)

PACX wraps Dataverse SDK operations with simpler syntax. All commands require an authenticated PAC session.

### Table Metadata

```powershell
# List all custom tables
pacx table list --solution DecisionsWorkspace

# Show table details
pacx table show --logical-name dft_governancebody

# Create a new table
pacx table create `
  --logical-name dft_newtable `
  --display-name "New Table" `
  --primary-column dft_newtableid `
  --owner-type UserOwned `
  --solution DecisionsWorkspace
```

### Column Operations

```powershell
# List columns on a table
pacx column list --table-name dft_governancebody

# Add a new column
pacx column add `
  --table-name dft_governancebody `
  --logical-name dft_newfield `
  --display-name "New Field" `
  --type String --max-length 200 `
  --solution DecisionsWorkspace
```

### Choice (Option Set) Operations

```powershell
# List choices
pacx choice list --solution DecisionsWorkspace

# Add a choice value
pacx choice add --name dft_meetingstatus; `
  --display-name "Meeting Status" --values Draft,Scheduled,InProgress,Completed,Cancelled
```

### Relationship Operations

```powershell
# List relationships for a table
pacx relationship list --table-name dft_governancebody

# Create a 1:N relationship
pacx relationship create `
  --one-table dft_governancebody `
  --many-table dft_meeting `
  --one-attribute dft_governancebodyid `
  --display-name "Governance Body" `
  --solution DecisionsWorkspace

# Create an N:N relationship
pacx relationship create-many-to-many `
  --table1 dft_committee `
  --table2 dft_member `
  --solution DecisionsWorkspace
```

### Workflow and Process

```powershell
# List workflows (classic + modern)
pacx workflow list --solution DecisionsWorkspace

# Activate a flow or process
pacx workflow activate --name "dft_OnNewMeeting"

# Deactivate
pacx workflow deactivate --name "dft_OnNewMeeting"
```

### Solution Script Support

```powershell
# Run a Dataverse SDK script from a file
pacx script --file scripts/dataverse/seed-sample-data.csx
```

## Script Wrappers

The following repository scripts wrap the above commands with validation, dry-run, and profile resolution:

| Script | CLI Equivalent | Dry-Run Support |
| --- | --- | --- |
| `scripts/Pack-Solution.ps1` | `pac solution pack` | `-DryRun` |
| `scripts/Unpack-Solution.ps1` | `pac solution unpack` | `-DryRun` |
| `scripts/Export-Solution.ps1` | `pac solution export` | No |
| `scripts/Import-Solution.ps1` | `pac solution import` | No (profile-gated) |
| `scripts/Invoke-SolutionChecker.ps1` | `pac solution check` | `-DryRun` |
| `scripts/New-ReleaseArtifact.ps1` | `pac solution pack` | `-DryRun` |

Always prefer the wrapper scripts for repo-consistent behaviour. Use raw `pac`/`pacx` commands only during development investigation or when the wrapper does not expose the required parameter.

## Version Notes

- `pac` detected at `C:\Users\60217257\.dotnet\tools\pac.exe` version `2.6.4.0`
- `pacx` detected at `C:\Users\60217257\.dotnet\tools\pacx.exe` version `1.2026.3.195`
- Run `pac install --version latest` to update PAC CLI
- Run `pacx update` to update PACX
