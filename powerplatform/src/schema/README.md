# Schema Build Plan

`schema-build-plan.json` is the checked-in build contract for the schema scaffold.

The PowerShell exporter reads the source YAML as text, extracts the stable pieces that matter for build ordering, and compares the result with the maintained JSON plan. That keeps the workflow deterministic without pulling in a separate YAML parser.

## What The Plan Contains

- Table creation order.
- Table logical names and primary fields.
- Choice dependencies per table.
- Relationship phases split into `mvp` and `future`.
- Connector-ready source fields stay in the table YAML so later Teams, Planner, and SharePoint binding can use stable IDs, sync state, and log metadata without renaming the domain model.
- Feature definition and override fields, when present in the table YAML, should stay stable so the per-meeting feature switchboard can resolve inherited defaults without renaming the schema contract.

## Workflow

1. Update the source YAML in `dataverse-tables.yaml`, `choices.yaml`, or `relationships.yaml`.
2. Run `scripts/Export-SchemaBuildPlan.ps1`.
3. Review the JSON diff if the schema shape changed.

## Compatibility Guard

`scripts/Test-SchemaBuildPlanCompatibility.ps1` validates the exporter against both LF and CRLF copies of the schema YAML. Keep this test wired into `scripts/Invoke-RepoValidation.ps1` so local validation catches the same line-ending differences that can appear on hosted Windows runners.

If the YAML becomes more irregular than the lightweight parser can safely understand, treat `schema-build-plan.json` as the canonical handoff artifact and update it directly so the build order stays explicit.
