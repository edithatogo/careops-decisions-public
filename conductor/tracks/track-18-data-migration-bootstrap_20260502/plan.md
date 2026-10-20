# Track 18 Plan

1. Document fixture pack design (small, medium, edge-case) with record counts and schema references.
2. Create fixture pack placeholder files that document the intended record shape and counts.
3. Add import mapping templates as markdown tables for:
   - Committee registers (source header → dft_governancebody columns).
   - Minutes registers (source header → dft_Minutes columns).
   - Paper registers (source header → dft_Paper columns).
   - Decision logs (source header → dft_Decision columns).
   - Action logs (source header → dft_ActionItem columns).
4. Add a bootstrap runbook for creating a clean development environment.
5. Add schema migration policy with change categories and review checklist.

## Dependencies

- Track 03 (schema YAML for column references).
- Track 09 (governance model for register relationships).
- Track 13 (deployment hardening for environment provisioning).

## Validation

- Fixture pack designs parse as valid record shapes.
- Import mappings reference known Dataverse table and column names.
