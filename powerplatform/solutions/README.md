# Solutions

This folder holds packed and unpacked Power Platform solution artifacts.

Target unmanaged solution name: `DecisionsWorkspace`.

Use:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Pack-Solution.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Unpack-Solution.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-ReleaseArtifact.ps1
```

Release packaging guidance lives in [docs/deployment/release-artifacts.md](../../docs/deployment/release-artifacts.md).

The release artifact script writes deterministic packages to:

- `artifacts/solutions/DecisionsWorkspace/managed/DecisionsWorkspace_managed.zip`
- `artifacts/solutions/DecisionsWorkspace/unmanaged/DecisionsWorkspace_unmanaged.zip`
