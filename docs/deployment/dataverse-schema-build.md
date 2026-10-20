# Dataverse Schema Build

Use this script when you want an ordered Dataverse schema build plan from the checked-in schema contract.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Invoke-DataverseSchemaBuild.ps1
```

## Safe Defaults

- The script is plan-only by default.
- `-WhatIf` is accepted through the standard PowerShell common parameter behavior.
- `-PlanOnly` keeps the run in review mode even if you experiment with other flags.
- `-Execute` requires `-ConfirmExecute` and still emits command templates rather than touching Dataverse.

## What It Reads

- `powerplatform/src/schema/schema-build-plan.json` for the build order.
- `powerplatform/src/schema/dataverse-tables.yaml` for column and alternate key details when present.

## Ordered Output

The script prints the build sequence in this order:

1. Choice sets.
2. Tables.
3. Primary fields.
4. Columns.
5. Keys.
6. Relationship phases, first `mvp` and then `future`.

If `pacx` is installed, the templates use that prefix. Otherwise the script falls back to `pac`. When neither is available, it still prints the templates so you can review them without a live environment.

## Recommended Use

1. Run the script with no flags and review the ordered output.
2. Confirm the generated column and key list matches the current schema sources.
3. If you are preparing a live session, rerun with `-Execute -ConfirmExecute` to get the execution-oriented template set.
4. Paste the templates into a live shell only after you have confirmed the environment and the exact CLI mapping.

## Example

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Invoke-DataverseSchemaBuild.ps1 -PlanOnly
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Invoke-DataverseSchemaBuild.ps1 -Execute -ConfirmExecute
```
