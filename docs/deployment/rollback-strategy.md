# Rollback Strategy

This document describes the rollback strategies available for the Decisions workspace. Every deployment or import must identify its rollback approach before execution.

## Rollback Methods

### Method 1: Overwrite Import (Preferred for Most Scenarios)

Import the previous solution version over the current one. This works when the schema change is additive (no destructive table/column removals).

```powershell
pac solution import `
  --path artifacts/solutions/DecisionsWorkspace_unmanaged_previous.zip `
  --settings-file powerplatform/settings/deployment-settings.local.json `
  --publish-changes
```

**When to use:** Most unmanaged updates where only new tables, columns, or flows were added.

**Risk:** Low. Previous state is restored from the same solution family.

**Data impact:** Records created under the new version remain in Dataverse; they are not removed by the import.

### Method 2: Environment Restore

Restore the target Power Platform environment from a backup taken before the import.

Requires System Administrator or Power Platform Administrator permissions.

**When to use:** Destructive schema changes (table or column deletion), data migration failures, or when the environment is in an unrecoverable state.

**Risk:** Medium. Full environment restore affects all solutions and data.

**Data impact:** All changes between the backup and the restore point are lost.

### Method 3: Managed Solution Roll-Forward

Import a previous managed solution version over the current managed layer.

```powershell
pac solution import `
  --path artifacts/solutions/DecisionsWorkspace_managed_previous.zip `
  --publish-changes
```

**When to use:** Managed deployments where layering is used.

**Risk:** Low-Medium. Managed solution layers can create dependency conflicts if components were upgraded across layers.

**Data impact:** None directly, but feature availability reverts.

## Rollback Decision Matrix

| Change Type | Recommended Method | Rollback Risk | Data Impact |
| --- | --- | --- | --- |
| New table or column | Overwrite import | Low | None |
| Modified column | Overwrite import | Low | Data in old format retained |
| Deleted table or column | Environment restore | Medium | Full data loss to backup point |
| New flow | Overwrite import | Low | Flow deactivated |
| Modified flow | Overwrite import | Low | Previous flow logic restored |
| Environment variables | Overwrite import with previous settings | Low | Values revert |
| Connection references | Overwrite import with previous settings | Low | Connections revert |
| Security roles | Overwrite import | Low | Role definitions revert |
| Seed data change | Reseed with previous data | Low | Data replaced |
| Settings import only | Re-import previous settings file | Low | Values revert |

## Rollback Checklist

Before any deployment:

- [ ] Previous solution zip is archived and accessible
- [ ] Environment backup exists if destructive changes are included
- [ ] Settings file backup exists
- [ ] Rollback plan is documented in the release notes
- [ ] Rollback owner is identified
- [ ] Rollback testing is completed (if applicable)

After rollback:

- [ ] Confirm solution version matches pre-deployment state
- [ ] Run smoke tests (SMK-01 through SMK-17)
- [ ] Verify connection references are intact
- [ ] Verify environment variable values are restored
- [ ] Capture evidence of the rollback in the deployment log

## Testing Rollback

For the development environment, test rollback after each release candidate:

1. Take a settings snapshot before import
2. Import the new version
3. Run smoke tests to confirm the new version works
4. Import the previous version
5. Run smoke tests to confirm the rollback is clean
6. Record results

## Current Rollback State

The current unmanaged solution baseline is `1.0.0.0`. Until a second version is deployed, rollback means re-importing from the `1.0.0.0` baseline zip. Archiving each release zip is required before the first deployment.
