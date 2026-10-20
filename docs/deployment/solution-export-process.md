# Solution Export Process

This document describes how to export the unmanaged `DecisionsWorkspace` solution from the Dylan development environment and prepare it for source control.

## Prerequisites

- Authenticated PAC session with the Dylan environment
- `pac` CLI available on PATH
- Write access to the `powerplatform/solutions/` directory

## Export Steps

### 1. Confirm the Active Environment

```powershell
pac auth list
```

Verify an active session shows the Dylan environment URL (`https://environment.example.invalid/`).

### 2. Export the Unmanaged Solution

```powershell
pac solution export `
  --name DecisionsWorkspace `
  --path powerplatform/solutions/DecisionsWorkspace_raw.zip `
  --managed false
```

The `--managed false` flag ensures the export is unmanaged. Do not use `--managed` or omit the flag for the source-controllable export.

### 3. Unpack the Exported Zip

```powershell
pac solution unpack `
  --zipfile powerplatform/solutions/DecisionsWorkspace_raw.zip `
  --folder powerplatform/solutions/DecisionsWorkspace `
  --packagetype Unmanaged
```

This overwrites the existing `powerplatform/solutions/DecisionsWorkspace/` folder with the live environment state.

### 4. Remove the Raw Zip

The raw zip contains environment-specific metadata and should not be committed:

```powershell
Remove-Item powerplatform/solutions/DecisionsWorkspace_raw.zip
```

### 5. Validate the Unpacked Source

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-SolutionBootstrap.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-AlmLayout.ps1
```

### 6. Pack a Round-Trip Check

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-ReleaseArtifact.ps1 -Unmanaged -DryRun
```

Review the output paths. If the pack succeeds, the unpack is valid for source control.

### 7. Commit

```powershell
git add powerplatform/solutions/DecisionsWorkspace/
git commit -m "solution: export unmanaged DecisionsWorkspace v{version} from Dylan"
```

## When to Re-Export

Re-export and unpack whenever:

- A maker makes changes directly in the Dylan environment (avoid when possible)
- Connection references or environment variables change in the live environment
- New tables, columns, or choices are added via the maker portal
- Before creating a managed release build

## When NOT to Re-Export

Do not re-export when:

- Only repo-local changes were made (scripts, docs, test files)
- The change is exclusively in the settings files (`powerplatform/settings/`)
- The change is exclusively in seed data (`powerplatform/src/seed/`)
- The change is exclusively in test artifacts (`tests/`)

Those items should be versioned through the repo directly and applied via settings import or seed import after solution import.

## Current Baseline

- Solution name: `DecisionsWorkspace`
- Display name: `CareOps Decisions`
- Version: `1.0.0.0` (baseline import on 2026-05-03)
- Publisher: `DecisionsWorkspacePublisher` (prefix `dft`)
- Source: `powerplatform/solutions/DecisionsWorkspace/`
- Status: The initial 1.0.0.0 export was imported and published. The unmanaged solution source in this repo matches that baseline. If the live environment has diverged through maker edits, re-export before making additional source changes.
