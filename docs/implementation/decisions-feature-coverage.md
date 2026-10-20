# Decisions Feature Coverage

This matrix maps Decisions-style meeting governance features to the current Decisions Workspace implementation.

| Capability | Status | Gate | Current Artifacts | Next Step |
| --- | --- | --- | --- | --- |
| Workspace home and launch navigation | Blocked | Dashboard publish pending | `HomeScreen`, `home_experience`, model-driven sitemap home entry | Publish a dashboard or canvas home page with launch cards. |
| Meeting series and meetings | Implemented | Built but needs setup records | `dft_MeetingSeries`, `dft_Meeting`, model-driven app navigation | Add committee-specific setup records and first-run dashboard. |
| Agendas | Implemented | Import path gated on NSW Outlook | `dft_AgendaItem`, agenda app screen spec, seed data | Add import path from NSW Outlook or SharePoint when available. |
| Papers and packs | Implemented | SharePoint folder convention pending | `dft_Paper`, SharePoint feature switch, paper register screen spec | Define SharePoint folder and naming convention. |
| Meeting invites | Blocked | Outlook connector/auth unstable | `outlook-invite-sync.yaml`, attendee records, Outlook connection reference | Activate after NSW Outlook connector or Graph mailbox/calendar access is stable. |
| Attendees, apologies, quorum | Implemented | Needs user guide and smoke scenario | `dft_Attendee`, membership, quorum fields, reporting definitions | Add user guide and smoke scenario for secretariat workflow. |
| Minutes | Implemented | Human review workflow pending | `dft_Minute`, minutes review screen spec, AI flow contract | Add human review workflow and Medication Safety example. |
| Transcript capture | Scaffolded | Graph transcript sync gated | `dft_Transcript`, transcript import feature switch, AI minutes flow | Keep manual import first; Graph transcript sync remains gated. |
| AI minutes | Blocked | License/capacity gate | `dft_AIBuilderPrompt`, `dft_AIBuilderRun`, `ai-transcript-minutes.yaml` | Confirm licensing/capacity, then enable for a reviewed pilot. |
| Formal decisions | Implemented | Chair approval workflow pending | `dft_Decision`, decision register views, reporting metrics | Add numbering and chair approval workflow for real committee usage. |
| Actions and follow-up | Implemented | Planner sync gated | `dft_ActionItem`, Planner sync flow contract | Enable Planner only after target plan and bucket are confirmed. |
| Voting and motions | Scaffolded | Committee-specific enablement | `dft_Vote`, voting feature switch, vote screen spec | Use only for committees that need formal voting records. |
| Conflicts of interest | Implemented | Pre-meeting checklist step pending | `dft_ConflictOfInterest`, meeting/audit model | Add workflow step to pre-meeting checklist. |
| Terms of reference | Implemented | Real record sourcing pending | `dft_TermsOfReference`, governance navigation, reporting | Add Medication Safety terms-of-reference sample or real record when sourced. |
| Committee and governance hierarchy | Implemented | Parent governance mapping pending | governance bodies, committees, memberships, reporting lines | Add Medical Safety / Medication Safety relationship to parent governance structure. |
| Access controls | Scaffolded | Security role hardening pending | `dft_AccessRule`, security/DLP docs, access rules navigation | Convert into security roles and team ownership rules during ALM hardening. |
| Teams posts | Blocked | Auth-gated / connector setup | Teams connection reference, teams summary flow contract | Publish only approved summaries when the meeting switch enables Teams posts. |
| SharePoint storage | Blocked | Site/library confirmation pending | SharePoint connection reference, paper/minutes feature switch | Confirm site/library and folder convention. |
| Planner integration | Blocked | Connector-bound / plan confirmation pending | Planner connection reference and flow contract | Confirm the DMS or committee plan before live sync. |
| Org hierarchy | Scaffolded | Permissions confirmation pending | Office 365 Users connection, org hierarchy sync contract | Use manager/direct-report data for reporting lines after permissions are confirmed. |
| Reporting and governance audit | Implemented | Live dashboard publish pending | Dataverse dashboard YAML, governance reporting docs | Surface live dashboards in the app and future Power BI/Fabric model. |
| Per-meeting configuration | Implemented | Quick-start instructions pending | feature definitions, series defaults, meeting overrides | Add quick-start instructions and admin health checks. |
| ALM and environment settings | Scaffolded | Env var definitions missing | connection references, settings files, GitHub Actions, PAC scripts | Add matching `decisions_*` environment variable definitions to the solution. |
| Medication Safety committee operating model | Implemented | Real record sourcing pending | `docs/operations/medication-safety-committee-workflows.md`, workflow docs | Import real committee record when NSW Outlook/SharePoint source is available. |
| Medication Safety synthetic seed data | Implemented | Validated with `-WhatIfOnly` | `powerplatform/src/seed/medication-safety-sample-data.json`, import script | Import to Dylan environment when sample data is ready. |
| Medication Safety meeting workflow | Implemented | NSW source gate | workflow docs, meeting/agenda/minutes/decision tables | Activate invite sync, agenda intake, transcript import, and approval flow. |
| Medication Safety left navigation and home | Implemented | Dashboard publish pending | model-driven sitemap, expandable headings | Publish a dashboard or canvas home page with Medication Safety launch cards. |
| Medication Safety AI Builder prompt contract | Scaffolded | License/capacity gate | AI Builder prompt and run tables, prompt contract docs | Enable live prompt execution after governance and licensing review. |
| Guide pack and Teams setup | Implemented | Graph/Teams auth gate | `docs/guides/decisions-workspace-quick-start.md`, manual Teams setup steps | Activate Teams tab automation when Graph authentication stabilises. |

## Built But Not Fully Used Yet

- Flow contracts exist, but most are not activated as live cloud flows.
- AI Builder prompt/run tables exist, but live prompt execution is disabled until governance and licensing are confirmed.
- Reporting definitions exist, and the app source now defines a home launchpad contract; the model-driven app still needs the dashboard or canvas home page to be published in the environment.
- Access rules exist as data, but full Dataverse security roles and team ownership still need hardening.
- Environment variable sample settings use `decisions_*`, while the manifest still needs to be reconciled and source-controlled in the solution.
- Medication Safety synthetic seed data exists and validates with `-WhatIfOnly`, but has not been imported into the Dylan environment.
- Medication Safety workflow documentation is written, but the end-to-end meeting lifecycle (invite sync through publish) is not yet activated in the live environment.
- Guide pack and Teams setup instructions exist, but automated Teams tab provisioning remains authentication-gated.
- Per-meeting feature switchboard is documented but has no admin health check or monitoring dashboard in the published app.

## Coverage Position

All major Decisions-style features are either implemented as Dataverse/app scaffolds or explicitly planned behind integration and access gates. The largest remaining gap is not data model coverage; it is making the published app coherent for real users through guided setup, live flow activation, Teams/SharePoint/Planner wiring, security roles, and committee-specific records.
