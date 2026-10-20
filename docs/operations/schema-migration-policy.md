# Schema Migration Policy

## Purpose

This policy governs how schema changes are made after data exists in the Dataverse environment. It defines change categories, review requirements, and the process for each type of change.

## Change Categories

### Safe Add (no review gate)

Changes that cannot break existing data or queries.

| Change Type | Example | Notes |
| --- | --- | --- |
| New table | Adding a new entity | Must not affect existing tables |
| New column (nullable) | Adding an optional text column | Default must be null |
| New choice value | Adding a new option to an existing choice set | Does not remove existing values |
| New relationship (optional) | Adding a new lookup column that is not required | Must not change delete behavior |
| New index or alternate key | Adding a new unique constraint on existing columns | Must not conflict with existing data |

**Process**: Implement directly in schema YAML, re-export build plan, run validation.

### Safe Rename (documentation review)

Changes that rename metadata without changing the logical model.

| Change Type | Example | Notes |
| --- | --- | --- |
| Rename display name | "Committee" → "Committee/Board" | Schema column name unchanged |
| Rename choice label | "Active" → "Currently Active" | Choice code unchanged |
| Update description | Improving field description text | No functional impact |

**Process**: Update YAML, update documentation, re-export build plan.

### Breaking Change (requires explicit review)

Changes that may break existing data, queries, or integrations.

| Change Type | Example | Risk |
| --- | --- | --- |
| Drop column | Removing a column | Data loss; Power Fx/formula errors |
| Make column required | Nullable → required | Import failures; existing nulls |
| Change column type | Text → choice | Conversion errors |
| Change delete behavior | Restrict → cascade | Accidental data loss |
| Drop table | Removing an entity | Complete data loss; app breakage |
| Drop choice value | Removing an option | Orphaned data in existing records |
| Rename logical column | `dft_name` → `dft_title` | Breaks all lookups, formulas, and imports |
| Change relationship cardinality | N:1 → 1:N | Schema rebuild required |

**Process**:
1. Document the change in an ADR (Architecture Decision Record).
2. Review the impact on:
   - Existing data (data loss assessment).
   - Canvas app formulas (Power Fx references).
   - Power Automate flows (column references in actions).
   - Import scripts (mapping templates).
   - Export/import plans (natural key changes).
3. Create a data migration script if existing data needs transformation.
4. Update schema YAML, re-export build plan.
5. Update import mapping templates if column names changed.
6. Apply migration to a test environment first.
7. Verify all tests pass before applying to shared environments.

## Review Checklist

For any breaking change:

- [ ] ADR documenting the change rationale is committed.
- [ ] Data loss assessment is complete.
- [ ] Canvas app formulas have been checked for affected column references.
- [ ] Power Automate flows have been checked for affected column references.
- [ ] Import mapping templates have been updated.
- [ ] Seed data has been updated to reflect the new schema.
- [ ] All validation scripts pass with the new schema.
- [ ] Migration has been tested in a non-production environment.

## Versioning

The schema version is tracked in `schemaVersion` in `dataverse-tables.yaml`. Increment on any breaking change.

| Version Change | When |
| --- | --- |
| Patch (0.1 → 0.1.1) | Safe add changes |
| Minor (0.1 → 0.2) | Safe rename changes |
| Major (0.1 → 1.0) | Breaking changes |

## Export Process

After any schema change:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Export-SchemaBuildPlan.ps1 -SchemaRoot powerplatform/src/schema -OutputPath powerplatform/src/schema/schema-build-plan.json

# Run compatibility validation
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-SchemaBuildPlanCompatibility.ps1
```

## Enforcement

- CI pipeline runs `Test-SchemaBuildPlanCompatibility.ps1` to detect unplanned drift.
- Breaking changes must not be committed without an accompanying ADR and review checklist completion.
- Schema YAML files are the source of truth; any environment-level schema changes should be backported to YAML.
