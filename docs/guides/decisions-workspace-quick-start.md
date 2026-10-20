# Decisions Workspace Quick Start

## Open The App

Use the Dylan environment app URL:

`https://environment.example.invalid/main.aspx?appid=22222222-2222-4222-8222-222222222222`

The app is a model-driven Dataverse workspace. The left navigation is the main way to work through records, and its headings are collapsible groups rather than a flat list.

## Add It To Teams

Until Teams tab automation is stable, add it manually:

1. Open Microsoft Teams.
2. Open the `Executive Team` team.
3. Open the target channel, usually `General`.
4. Select `+` to add a tab.
5. Choose `Website`.
6. Name the tab `Decisions Workspace`.
7. Paste the app URL above.
8. Save.

If Power Apps appears as an available tab type and accepts the app, use that instead of Website. Website remains the fallback because it does not depend on Graph tab automation.

### Troubleshooting the Teams Tab

If the Website tab type is not available:
- Ask your Teams administrator to enable custom tabs or the Website app.
- Verify the app URL is accessible from the Teams client (no conditional access or IP restrictions).
- Check that the channel allows custom tabs in the Teams admin centre.

Once added, right-click the tab and select **Pin** to keep it visible in the channel navigation.

## Main Areas

The source contract and the generated guide both use the same expandable headings:

- Meetings: meeting list, meeting workspace, agenda, attendance, and papers.
- Governance: governance bodies, meeting series, terms of reference, memberships, people, and reporting lines.
- Actions: follow-up actions, ownership, due dates, escalation, and Planner binding state.
- Decisions: minutes, formal decisions, votes, and publication review.
- Papers: paper register, attachments, and transcript-linked supporting material.
- Help: the quick-start guidance, role-based first steps, and source rules.

## Using the Left Navigation

The left navigation uses expandable headings. Click a heading to expand or collapse its sub-items:

- **Meetings**: meeting list, workspace, agenda, attendance, papers.
- **Governance**: governance bodies, committees, memberships, terms of reference, reporting lines.
- **Actions**: follow-up items, ownership, due dates, escalation, Planner binding state.
- **Decisions**: minutes, formal decisions, votes, publication review.
- **Papers**: paper register, attachments, supporting material.
- **Help**: quick-start guidance, role checklists, source rules.

Each expanded heading persists across sessions. This is the primary way to navigate between record types.

## Home Page Workflow

Use Workspace Home as the first stop. It should show the next practical actions rather than just the table list:

- Prepare meeting.
- Build agenda.
- Add papers.
- Confirm attendance.
- Draft minutes.
- Review decisions and actions.
- Configure committee.
- Open feature switchboard.
- Review AI drafts.
- Check integrations.

For the model-driven app, these launch cards are the target home-page build contract. Until a custom dashboard or canvas home page is published, use the left navigation items with the same names. The generated quick-start page mirrors this contract with expandable headings so the navigation model stays visible even before the home page is customized.

## Role-Based First Steps

Chair:

- Review the meeting record before the meeting.
- Confirm agenda items, decision items, conflicts, and quorum.
- Review draft minutes, decisions, and actions after the meeting.
- Approve official minutes only after secretariat review.

First-run checklist:
- [ ] Confirm your access to the Decisions Workspace app and Teams tab.
- [ ] Review the meeting series and feature defaults are configured.
- [ ] Check that the committee governance body and terms of reference are recorded.
- [ ] Verify membership records for chair, secretariat, and voting members.
- [ ] Review the meeting workflow and escalation process.

Secretariat:

- Maintain the meeting series and meeting records.
- Add agenda items, papers, attendees, apologies, and conflicts.
- Import or attach transcript and notes when available.
- Review AI-generated drafts before chair approval.
- Publish approved minutes and follow-up actions when enabled.

First-run checklist:
- [ ] Create or confirm the meeting series, governance body, and terms of reference.
- [ ] Add chair, secretariat, and member records to the membership list.
- [ ] Configure feature defaults for the meeting series.
- [ ] Schedule the first meeting and add agenda items.
- [ ] Test the manual Teams tab setup.
- [ ] Run the sample data import script with `-WhatIfOnly` to validate the schema.
- [ ] Review the integration logs after any import or connector test.

Member:

- Read agenda, papers, minutes, and assigned actions.
- Update action status.
- Record conflicts or apologies through the agreed workflow.

First-run checklist:
- [ ] Confirm access to the Decisions Workspace app and Teams tab.
- [ ] Review your membership record and role.
- [ ] Read the agenda and papers before the first meeting.
- [ ] Record conflicts or apologies if applicable.
- [ ] Review assigned actions and update status as needed.

Administrator:

- Configure feature switches by meeting series or meeting.
- Maintain access rules and integration settings.
- Monitor integration logs and AI Builder review queues.

First-run checklist:
- [ ] Confirm the app is imported, published, and accessible in the Dylan environment.
- [ ] Configure feature defaults for each meeting series.
- [ ] Review and update access rules for chair, secretariat, members, and observers.
- [ ] Check connection references for Teams, SharePoint, Planner, and Outlook.
- [ ] Verify environment variables are set correctly for the target environment.
- [ ] Monitor integration logs after any configuration change.
- [ ] Review AI Builder prompt settings if drafting is enabled.

## Feature Switches

Features can be configured by meeting series and overridden for a single meeting. Current switches include voting, AI minutes, transcript import, Planner sync, Teams posts, SharePoint papers and minutes, Outlook invites, and org hierarchy.

Disabled features should show as explicit skip paths. They should not fail silently or create partial records.

## Sample Data vs. Real Data

The workspace includes synthetic sample records for the Medication Safety Committee. These are clearly marked and use placeholder values. Find them in:

- **Dataverse tables**: records with source = "Sample" or "(Sample)" name prefix.
- **Repo seed file**: `powerplatform/src/seed/medication-safety-sample-data.json`.
- **Validation script** (dry-run only):
  ```powershell
  powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Import-MedicationSafetySampleToDataverse.ps1 -WhatIfOnly
  ```

Synthetic records include placeholder attendees, agenda items, papers, minutes, decisions, actions, feature settings, and integration logs. They demonstrate the schema without real committee data.

Real Medication Safety Committee records must come from NSW Outlook, SharePoint, Teams, or user-provided exports. Gmail is not a valid source. Before importing real data:
1. Confirm the source system and authorization.
2. Record source, source date, and import reviewer.
3. Keep original documents in the approved NSW repository.

## Current Limits

- See `docs/deployment/restricted-runtime-matrix.md` for the full restricted-versus-live capability split.
- The app is published in Dylan, but Teams tab automation remains authentication-gated.
- Connector references have been bound, but full environment-variable settings import still needs matching solution environment variable definitions.
- AI Builder minutes should stay disabled until capacity, prompt governance, and human review are confirmed.
- Real committee records must come from NSW Outlook, SharePoint, Teams, or user-provided exports. Gmail is not a valid source for the NSW committee workflow.
- **AI Builder**: Live AI minute drafting is disabled until capacity, licensing, prompt governance, and human review are confirmed. The AI Builder prompt/run tables exist but are not connected to live flows.
- **Real-data gate**: NSW Outlook, SharePoint, or Teams source access is not yet available. All committee records remain synthetic until one of these sources is authorized.
- **Connector blockers**: Teams, Planner, and SharePoint connection references are bound but not activated. Graph authentication has been unstable. Full connector wiring requires environment-level permissions.
- **Environment variables**: The solution does not yet include matching `decisions_*` environment variable definitions. Full settings import is blocked until these are source-controlled and deployed.
- **Security roles**: Dataverse security roles and team ownership rules are designed but not hardened. Current access relies on the model-driven app's built-in sharing.
