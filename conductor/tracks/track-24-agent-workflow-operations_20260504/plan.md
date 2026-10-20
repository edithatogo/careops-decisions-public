# Track 24 Plan

## Workstreams

1. Agent ownership model
   - Define agent lanes and file ownership.
   - Separate local source changes from live Dylan operations.
   - Document which agents can work in parallel and which must wait.

2. Writer and reviewer pairing
   - Pair each high-risk writer lane with a reviewer lane.
   - Require reviewer checks for generated pages, solution imports, ALM changes, seed/import scripts, and governance-sensitive content.
   - Keep reviewer output evidence-based: files changed, commands run, risks found, and remaining blockers.

3. Dylan verification
   - Add a read-only verification script for app module, generated pages, app tables, and optional Medication Safety sample records.
   - Support JSON output for CI or handoff logs.
   - Keep the script free of tokens and live write operations.

4. Deployment serialization
   - Route live PAC imports, generated-page uploads, publish operations, and sample-data writes through one deployment operator.
   - Allow one reviewer to run read-only verification in parallel after the deployer reports completion.
   - Treat Teams, Planner, SharePoint, Graph, and Outlook activation as environment-gated unless connector references and permissions are verified.

5. Conductor integration
   - Add Track 24 references to the track index, Conductor status, roadmap, source map, and planning validation.
   - Keep the runbook discoverable from operations docs.

## Parallelization

- Agent A: Model-driven app UX and generated pages.
- Agent B: Dataverse data model and seed/import scripts.
- Agent C: Flow and Microsoft 365 integration contracts.
- Agent D: ALM, CI, solution checker, and release artifacts.
- Agent E: governance, privacy, access, and NSW-source gates.
- Agent F: reviewer/tester pass over A-D outputs.

Live deployment mode is intentionally narrower:

- Deployer: one agent runs PAC/PACX live writes and records exact commands.
- Reviewer: one agent runs read-only verification after publish completes.
- Other agents continue local docs/tests only.

## Done Definition

- Track files exist and validate.
- `docs/operations/agent-runbook.md` defines the agent workflow.
- `scripts/Verify-DylanDeployment.ps1` runs in read-only mode and supports JSON output.
- Planning validation includes Track 24.
- Repo validation passes.
