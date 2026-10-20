# Agent Runbook

Use this runbook when parallelizing Decisions Workspace work across subagents or separate implementation lanes.

## Operating Rule

Parallelize source work; serialize live Dylan writes.

Only one deployment operator should run live `pac solution import`, `pac model genpage upload`, publish operations, Teams tab changes, SharePoint/Planner/Graph writes, or Dataverse sample-data writes at a time. A reviewer can run read-only verification after the deployer reports completion.

Real Medication Safety Committee records must come from NSW Outlook, SharePoint, Teams, or user-provided exported files. Gmail is not a valid source for NSW committee records.

## Agent Lanes

| Agent | Owns | Typical files | Tools |
| --- | --- | --- | --- |
| Power Platform ALM Agent | Solution packaging, imports, exports, solution checker, CI, releases, ALM settings | `.github/workflows/*`, `scripts/Invoke-SolutionChecker.ps1`, `scripts/Import-Solution.ps1`, `powerplatform/settings/*`, `powerplatform/solutions/*` | `pac solution`, `pac env`, `pac model`, GitHub Actions, solution checker |
| Model-Driven App UX Agent | App navigation, generated pages, forms/views, guides, user workflow polish | `powerplatform/src/app/*`, `powerplatform/src/genpages/*`, app module sitemap/source, `docs/guides/*` | `pac model genpage`, solution pack/import, repo validation |
| Dataverse Data Model Agent | Tables, choices, relationships, seed data, import/upsert helpers | `powerplatform/src/schema/*`, `powerplatform/src/seed/*`, `scripts/Import-*Seed*.ps1`, generated Dataverse docs | `pac env fetch`, Dataverse Web API helpers, seed validation |
| Workflow Integration Agent | Planner, Teams, SharePoint, Outlook, Office 365 Users, AI Builder, Graph, flow contracts | `powerplatform/src/flows/*`, `powerplatform/src/flow-contracts.md`, `powerplatform/settings/environment-variable-manifest.json` | Power Automate contracts, connector manifests, Graph/PAC reads |
| Governance and Safety Agent | Access, DLP, retention, audit, NSW source gates, human review | `docs/readiness/*`, `docs/operations/*`, `docs/architecture/ai-builder-minutes-pattern.md`, access matrix | privacy review, access matrix checks, source-gate review |
| Medication Safety Secretariat Agent | Committee operating records and secretariat workflow | `docs/operations/medication-safety-committee-workflows.md`, `powerplatform/src/seed/medication-safety-sample-data.json` | seed validation, workflow review, source-gate checks |
| Testing and Review Agent | Repo validation, solution checker, smoke checks, deployment evidence | `tests/*`, `scripts/Test-*.ps1`, `scripts/Invoke-RepoValidation.ps1`, `scripts/Verify-DylanDeployment.ps1` | repo validation, solution checker, read-only PAC checks |
| Deployment Operator Agent | Dylan deployment, generated page upload, publish, verification record | deployment scripts, release artifacts, generated page upload command logs | `pac auth`, `pac solution import`, `pac model genpage`, `pac env fetch` |

## Pairing Model

Use writer/reviewer pairs for high-risk changes:

| Writer | Reviewer | Required evidence |
| --- | --- | --- |
| Model-Driven App UX Agent | Testing and Review Agent | Generated page list, app navigation impact, repo validation |
| Dataverse Data Model Agent | Governance and Safety Agent | Seed source status, no real data leakage, natural keys, validation output |
| Workflow Integration Agent | Governance and Safety Agent | Connector gates, feature switch behavior, integration log path |
| Power Platform ALM Agent | Testing and Review Agent | Pack/import commands, solution checker, CI status, rollback path |
| Deployment Operator Agent | Testing and Review Agent | Exact target environment, commands, app/page IDs, read-only verification output |

## Parallel Work Rules

- Keep write sets disjoint. Do not let multiple agents edit the same solution XML, generated page, seed file, or settings manifest in parallel.
- Prefer source contracts first: YAML, JSON, Markdown, TypeScript generated page source, and scripts.
- Treat `artifacts/` as generated evidence unless the repo already tracks the file intentionally.
- Do not commit secrets, bearer tokens, connector IDs that are not already intended as environment settings, or real confidential committee documents.
- For generated pages, commit the source file and generated runtime types when PAC creates them.
- For solution imports, record the target environment URL and app ID in the handoff.

## Evidence Expectations

Each agent handoff must include the following evidence for its lane:

| Agent Lane | Required evidence |
| --- | --- |
| Power Platform ALM Agent | Files packed, solution checker output (exit code + summary), target environment URL, rollback path documented, settings file validated against manifest |
| Model-Driven App UX Agent | Generated page list (`pac model genpage list` output), app module ID, sitemap changes, repo validation passed, before/after navigation structure |
| Dataverse Data Model Agent | Tables changed (schema names + keys), seed payload validated (natural keys, no real data leakage), relationship matrix, import script dry-run output |
| Workflow Integration Agent | Flow contracts updated (YAML + flow-catalog.yaml), connector gates verified, environment variable references match manifest, feature switch defaults set |
| Governance and Safety Agent | Privacy review notes, NSW source gate status, access matrix updated, AI Builder review gate confirmed, DLP scope documented |
| Medication Safety Secretariat Agent | Sample records validated (synthetic only), committee workflow steps confirmed, source-gate labels present, no real NSW data in sample set |
| Testing and Review Agent | Repo validation output, solution checker results, smoke test results, verification script output, remaining-risk log |
| Deployment Operator Agent | Exact `pac` commands executed, target environment URL, app ID, generated page IDs, import/publish result, read-only verification output from `scripts/Verify-DylanDeployment.ps1` |

## Live Dylan Rules

Before any live write:

- Confirm target URL is `https://environment.example.invalid/`.
- Confirm the account is `operator@example.invalid`.
- Run `scripts/Verify-DylanDeployment.ps1 -Json` and capture the output as the pre-deployment baseline.
- Confirm whether the operation writes source, app metadata, generated pages, Dataverse data, Teams, Planner, SharePoint, Outlook, or AI Builder.

During live deployment:

- One deployment operator runs the write.
- One reviewer waits for completion and then runs read-only verification.
- Other agents continue only local work.

After live deployment:

- Record app ID, generated page IDs, environment URL, import/publish result, and verification output.
- If the deployment was interrupted, first check whether the command is still running before starting a new import.
- If remote host or token refresh fails, do not claim success. Verify with `pac env fetch` or `pac model genpage list`.

## Review Checklist

- Repo validation passed.
- Solution checker passed when solution contents changed.
- Dylan verification passed or the live blocker is recorded.
- Real-data source rule is respected.
- AI output remains draft until human review.
- Disabled integrations have explicit skip behavior.
- App navigation or generated pages are actually visible in Dylan, not only documented in the repo.
