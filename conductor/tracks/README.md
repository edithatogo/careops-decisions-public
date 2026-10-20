# Tracks

| Track | Purpose | Parallel Phase |
| --- | --- | --- |
| `track-00-conductor-tooling-inventory_20260502` | Setup, tools, repo structure | First |
| `track-01-research-feature-map_20260502` | Decisions public feature mapping | Phase 1 |
| `track-02-template-kit-evaluation_20260502` | Templates, kits, samples, tests | Phase 1 |
| `track-03-power-platform-architecture_20260502` | Dataverse, flows, security, storage | Phase 1 |
| `track-08-microsoft-platform-integration_20260502` | Microsoft 365 connector and Graph map | Phase 1 |
| `track-04-alm-test-harness_20260502` | Validation, smoke tests, Test Engine | Phase 2 |
| `track-05-canvas-app-mvp_20260502` | Power Apps Teams tab MVP source contracts complete | Phase 2 |
| `track-06-automation-integrations_20260502` | Power Automate and connector flow contracts complete | Phase 2 |
| `track-09-governance-model_20260502` | Committees, terms, hierarchy, quorum | Phase 2 |
| `track-10-meeting-records_20260502` | Agendas, papers, minutes, registers | Phase 2 |
| `track-11-invites-attendees_20260502` | Invites, attendees, apologies, quorum | Phase 2 |
| `track-12-ai-transcripts-minutes_20260502` | Manual transcript and AI minutes | Phase 2 |
| `track-13-deployment-hardening-alm-entra-readiness_20260502` | Deployable MVP hardening, ALM, security, tests, Azure/Entra gates | Phase 3 |
| `track-14-alm-solution-release_20260502` | ALM, solution packaging, release and rollback | Phase 3 |
| `track-15-security-dlp-retention_20260502` | Security, DLP, privacy, retention, external participants | Phase 3 |
| `track-16-governance-reporting_20260502` | Dataverse reporting, dashboards, Power BI/Fabric model | Phase 3 |
| `track-17-operations-monitoring-support_20260502` | Operations, monitoring, support runbooks, continuity | Phase 3 |
| `track-18-data-migration-bootstrap_20260502` | Data migration, seed fixtures, environment bootstrap | Phase 3 |
| `track-19-test-automation-ci_20260502` | Test automation, connector mocks, CI, evidence | Phase 3 |
| `track-20-decisions-readiness-gates_20260502` | ADRs, licensing, permissions, service accounts, readiness gates | Phase 3 |
| `track-21-alm-activation-ci_20260502` | ALM activation manifests, solution checker, CI validation | Phase 3 |
| `track-22-mvp-activation-enhancements_20260503` | Dataverse-first app-core before deployment: MVP app/flow activation, release artifacts, solution checker, sample data, wizard, review gates, reporting, tests, and local implementation hardening | Phase 3 |
| `track-23-medication-safety-operationalization_20260504` | Medication Safety operationalization, guide pack, Decisions feature coverage, AI Builder minutes, approved deployment-source gates, and ALM settings closure | Phase 3 |
| `track-24-agent-workflow-operations_20260504` | Agent lane ownership, writer/reviewer pairing, live deployment serialization, and maintainer verification | Phase 3 |
| `track-25-credentialing-governance-extension_20260829` | Feature-flagged credentialing committee, recommendation, delegated-decision, conditions, review, and appeal capability pack | Phase 4 |
| `track-07-future-azure-entra-upgrade_20260502` | Graph, Entra, Azure, Teams extensions design boundary complete | Future |

Phase 1 tracks can run in parallel after setup. Phase 2 implementation tracks should use the architecture and test harness as the contract. Phase 3 turns the scaffold into a deployable MVP while keeping Azure/Entra implementation gated until access is available. Track 13 is the umbrella; tracks 14-20 are the parallel execution split, track 21 covers repo-side ALM activation and CI, and track 22 covers the Dataverse-first app-core build work on top of the completed v1.0.0 Dataverse source export before live Teams/SharePoint deployment.

The current implementation wave is local hardening across seed payload validation, app contract validation, flow contract validation, reporting/data quality, operations readiness, and Conductor status. Track 23 adds the user guide, feature coverage, AI Builder, and Medication Safety operationalization layer, with real-data ingestion gated to approved deployment sources. Track 24 formalizes agent lanes, writer/reviewer pairing, live deployment serialization, and maintainer verification. Live Teams, SharePoint, Planner, and Graph deployment blockers remain environment-gated work outside the local build-complete signal. Track 25 adds the credentialing governance extension as a disabled-by-default capability pack and preserves separate committee recommendation, delegated decision, authoritative register, and process-assurance boundaries.
