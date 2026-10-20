# Reporting Smoke Scenarios

These scenarios validate that core Dataverse views and app dashboards return
expected results with seeded data. Run after seed data import and before
sign-off.

## Secretary Workbench

| # | Scenario | Expected | Data Dependencies |
| - | -------- | -------- | ----------------- |
| 1 | Secretary opens Meeting Pipeline view | Shows seeded meetings with start dates within next 30 days | At least one meeting with `dft_startdatetime` within 30 days |
| 2 | Secretary opens Missing Agenda view | Shows seeded meetings without linked agenda items | At least one meeting without any `dft_AgendaItem` |
| 3 | Secretary opens Draft Minutes Pending Review view | Shows seeded minutes in Draft or Chair Review status | At least one `dft_Minutes` row with status=Draft or Chair Review |
| 4 | Secretary checks Unapproved Minutes With Publish URL | Returns zero (clean) or identifies seeded records | Any `dft_Minutes` with non-approved status and populated `dft_publishedurl` |
| 5 | Secretary verifies `meetingsScheduledNext30Days` metric | Count matches expected from seed data | Seed contains known meeting count |
| 6 | Secretary verifies `minutesPendingReviewCount` metric | Count matches expected from seed data | Seed contains known minutes count |

## Chair Review

| # | Scenario | Expected | Data Dependencies |
| - | -------- | -------- | ----------------- |
| 7 | Chair opens Minutes Awaiting Approval view | Shows seeded minutes in Chair Review with no approval timestamp | At least one `dft_Minutes` with status=Chair Review and `dft_approvedon` null |
| 8 | Chair opens Open Decisions view | Shows seeded proposed or deferred decisions | At least one `dft_Decision` with status=Proposed or Deferred |
| 9 | Chair opens AI Review Queue view | Shows seeded AI runs with completed status and pending human review | At least one `dft_AIBuilderRun` with `dft_runstatus=Completed` and `dft_humanreviewstatus=Pending` |
| 10 | Chair checks `minutesAwaitingChairApproval` metric | Count matches expected | Known minutes in Chair Review state |

## Member Actions

| # | Scenario | Expected | Data Dependencies |
| - | -------- | -------- | ----------------- |
| 11 | Member opens My Open Actions view | Shows only actions assigned to the signed-in member | Seed has actions with `dft_ownerid` pointing to test person |
| 12 | Member opens Decisions Assigned To Me view | Shows only decisions assigned to the signed-in member | Seed has decisions with `dft_ownerid` pointing to test person |
| 13 | Member checks `myOpenActionCount` metric | Count matches only own open actions | Known owned action count |
| 14 | Member attempts to access Secretary Workbench | Access denied or dashboard is not visible | Roles correctly scoped |

## Governance Audit

| # | Scenario | Expected | Data Dependencies |
| - | -------- | -------- | ----------------- |
| 15 | Auditor opens Access Exceptions view | Shows seeded exception or restricted access rules | At least one `dft_AccessRule` with `dft_ruletype=Exception` or `dft_classification=Restricted/Legal` |
| 16 | Auditor opens Open Integration Errors view | Shows seeded failed or retried integration logs | At least one `dft_IntegrationLog` with status=Failed or Retried |
| 17 | Auditor opens Records Missing Owner view | Shows seeded records that lack owner lookups | At least one governance body, decision, action, or paper with null owner |
| 18 | Auditor checks `openIntegrationErrorCount` metric | Count matches | Known failed/retried integration logs |

## Governance Configuration Health

| # | Scenario | Expected | Data Dependencies |
| - | -------- | -------- | ----------------- |
| 19 | Admin opens Governance Hierarchy view | Shows all seeded governance bodies with parent, type, and status | Seed contains known governance body hierarchy |
| 20 | Admin opens Feature Switch Defaults view | Shows seeded feature definitions with their default states | Seed contains `dft_FeatureDefinition` rows |
| 21 | Admin opens Connector Health Summary view | Shows recent integration activity across connectors | Seed contains `dft_IntegrationLog` rows with varied timestamps |
| 22 | Admin checks `governanceHierarchyBodyCount` metric | Count matches expected | Known governance body count |

## Security Access

| # | Scenario | Expected | Data Dependencies |
| - | -------- | -------- | ----------------- |
| 23 | Administrator creates a new governance body record | Record created; visible to secretary of that body | Administrator role assigned |
| 24 | Secretary edits a paper record | Update succeeds | Secretary role assigned; paper exists |
| 25 | Member edits a paper record they did not author | Update fails | Member role assigned; paper owned by another user |
| 26 | Auditor views a restricted decision | View succeeds | Auditor role assigned; decision exists |
| 27 | Guest Presenter views meeting pipeline | View is empty or scoped only to their assigned meeting | Guest Presenter role assigned |
| 28 | Guest Presenter views a transcript | View is empty | Transcript not in scope for Guest Presenter role |

## Acceptance Criteria

- All secretary, chair, member, auditor, and admin smoke scenarios pass.
- Security-role-gated scenarios (Member accessing Secretary view, Guest
  Presenter accessing transcripts) correctly block access.
- Metrics return non-null counts that match seed data expectations.
- Each dashboard displays without error for its intended audience.
