# Bootstrap Runbook

## Purpose

Create a clean Dataverse development environment from scratch with schema, sample data, and validation.

## Prerequisites

- Access to a Dataverse environment (Power Platform trial, developer plan, or Dataverse for Teams)
- PowerShell 7+
- Azure CLI (`az`) with Dataverse access token capability
- Git clone of this repository
- Schema build plan exported (`scripts/Export-SchemaBuildPlan.ps1`)

## Steps

### 1. Provision Environment

If a clean environment does not exist:

- **Power Platform admin center**: Create a new environment with Dataverse database.
- **or Developer Plan**: https://powerapps.microsoft.com/developerplan
- **or Dataverse for Teams**: Create within Teams (MVP mode).

Record the environment URL (e.g. `https://orgXXXXXXX.crm6.dynamics.com`).

### 2. Configure Solution Publisher

In the Power Platform maker portal:
- Navigate to Solutions → Default Solution → Publisher.
- Set publisher prefix to `dft` (or verify it matches the schema publisherPrefix).

### 3. Build Schema

Run the schema build plan to create tables, columns, and relationships:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Invoke-DataverseSchemaBuild.ps1 -EnvironmentUrl "https://orgXXXXXXX.crm6.dynamics.com" -DryRun

# Remove -DryRun to execute
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Invoke-DataverseSchemaBuild.ps1 -EnvironmentUrl "https://orgXXXXXXX.crm6.dynamics.com"
```

The schema build uses the exported `powerplatform/src/schema/schema-build-plan.json` as its source of truth.

### 4. Validate Schema

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-SchemaBuildPlanCompatibility.ps1
```

### 5. Import Sample Data

Choose a fixture pack:

```powershell
# Small fixture (smoke test)
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-SeedData.ps1 -SeedPath powerplatform/src/seed/fixture-small.json -WritePayloads

# Medium fixture (full hierarchy)
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-SeedData.ps1 -SeedPath powerplatform/src/seed/fixture-medium.json -WritePayloads

# For live Dataverse writes (requires Azure CLI auth):
# This script generates a deterministic import plan at artifacts/seed/
```

For live Dataverse writes with `Import-MedicationSafetySampleToDataverse.ps1`:

```powershell
# What-if only (validates structure without writing)
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-MedicationSafetySampleToDataverse.ps1 -WhatIfOnly

# Live import (requires Azure CLI auth)
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-MedicationSafetySampleToDataverse.ps1 -EnvironmentUrl "https://orgXXXXXXX.crm6.dynamics.com"
```

### 6. Validate Import Plan

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-SeedImportPlan.ps1
```

### 7. Verify Data

Open the environment in Power Apps or the maker portal and verify:
- Tables exist with correct columns.
- Sample data appears in table views.
- Relationships are created (lookup columns present).
- Choice values are populated.

### 8. Smoke Test

Run the repo-level validation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Invoke-RepoValidation.ps1
```

## Resetting

### Full Environment Reset

Use when the entire environment is disposable and a clean slate is preferred:

```powershell
# 1. Delete the solution (removes all custom tables, flows, and apps)
pac solution delete --solution-name DecisionsWorkspace

# 2. Or delete the entire environment via Power Platform admin center
#    Create a fresh environment and start from Step 1.

# 3. Re-run full bootstrap from Step 3.
```

### Data-Only Reseed

Use when the schema is stable but sample data needs to be refreshed. Data is deleted in reverse dependency order, then re-imported via fixture packs:

```powershell
# 1. Delete seeded data in reverse dependency order using PACX
pacx data delete --table dft_IntegrationLog --filter "dft_name like 'seed-%'"
pacx data delete --table dft_AIBuilderRun --filter "dft_name like 'seed-%'"
pacx data delete --table dft_VoteResponse --filter "dft_name like 'seed-%'"
pacx data delete --table dft_Vote --filter "dft_name like 'seed-%'"
pacx data delete --table dft_ConflictOfInterest --filter "dft_name like 'seed-%'"
pacx data delete --table dft_AccessRule --filter "dft_name like 'seed-%'"
pacx data delete --table dft_ActionItem --filter "dft_name like 'seed-%'"
pacx data delete --table dft_Decision --filter "dft_name like 'seed-%'"
pacx data delete --table dft_Minutes --filter "dft_name like 'seed-%'"
pacx data delete --table dft_Transcript --filter "dft_name like 'seed-%'"
pacx data delete --table dft_Paper --filter "dft_name like 'seed-%'"
pacx data delete --table dft_AgendaItem --filter "dft_name like 'seed-%'"
pacx data delete --table dft_Attendee --filter "dft_name like 'seed-%'"
pacx data delete --table dft_MeetingFeatureOverride --filter "dft_name like 'seed-%'"
pacx data delete --table dft_Meeting --filter "dft_name like 'seed-%'"
pacx data delete --table dft_MeetingSeries --filter "dft_name like 'seed-%'"
pacx data delete --table dft_FeatureTemplate --filter "dft_name like 'seed-%'"
pacx data delete --table dft_FeatureDefinition --filter "dft_name like 'seed-%'"
pacx data delete --table dft_CommitteeMembership --filter "dft_name like 'seed-%'"
pacx data delete --table dft_TermsOfReference --filter "dft_name like 'seed-%'"
pacx data delete --table dft_OrgRelationship --filter "dft_name like 'seed-%'"
pacx data delete --table dft_OrgPerson --filter "dft_name like 'seed-%'"
pacx data delete --table dft_GovernanceBody --filter "dft_name like 'seed-%'"

Write-Host "Seed data deleted. Re-importing fixture packs..."

# 2. Import fixture packs in dependency order
.\scripts\Import-SeedData.ps1 -SeedPath powerplatform/src/seed/fixture-small.json -WritePayloads
.\scripts\Import-SeedData.ps1 -SeedPath powerplatform/src/seed/fixture-medium.json -WritePayloads
.\scripts\Import-SeedData.ps1 -SeedPath powerplatform/src/seed/fixture-edge.json -WritePayloads

# 3. Validate import
.\scripts\Test-SeedImportPlan.ps1
```

### Checkpoint-Based Restore

If Dataverse versioning or solution-layer backup is available:

```powershell
# Export current solution state before reseeding
.\scripts\Export-Solution.ps1 -SolutionName DecisionsWorkspace -Path artifacts/backups/pre-reseed-backup.zip

# After reseed, compare with baseline
.\scripts\Invoke-RepoValidation.ps1 -RequireImplementationScaffolds
```

## Troubleshooting

| Issue | Likely Cause | Resolution |
| --- | --- | --- |
| "Publisher prefix mismatch" | Schema expects `dft` prefix | Update publisher prefix in target environment |
| "Table not found" | Schema build not complete | Re-run Invoke-DataverseSchemaBuild.ps1 |
| "Natural key mismatch" | Seed data key doesn't match import plan | Check seed JSON for alternate key values |
| "Azure CLI auth failed" | No Dataverse token available | Run `az login` and verify environment URL |
| "Choice value not found" | Choice not yet imported | Verify choices.yaml is in scope for schema build |
