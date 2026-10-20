# Support Runbooks

Use these runbooks when a failure scenario is detected. Each runbook defines
detection, triage, response, and escalation path. Read `docs/operations/README.md`
for first-triage guidance before diving into a specific runbook.

---

## Runbook: Failed Flows

**Detection**: Power Automate run history shows a failed run. IntegrationLog
has rows with `dft_status=Failed`. User reports that a sync did not happen.

**Triage**:
1. Open Power Automate run history and copy the run ID, failed action,
   connector name, and error message.
2. Open the IntegrationLog for the related correlation ID.
3. Classify the failure:
   - **Connector**: Connection reference expired, permissions changed, or
     connector is blocked by DLP.
   - **Payload**: Schema mismatch, missing required field, invalid format.
   - **Permission**: Connected user no longer has access to the target
     system (mailbox, site, plan, team).
   - **Transient**: Throttling, timeout, service outage.

**Response**:
| Cause | Action |
| ----- | ------ |
| Expired connection reference | Re-authenticate the connection and update the connection reference in deployment settings. |
| DLP block | Confirm DLP policy allows all connectors used by the flow. Request DLP exception if needed. |
| Schema mismatch | Compare the flow action schema with the current Dataverse table definition. Update flow or schema. |
| Permission changed | Confirm the connection owner still has access to the target resource. Reassign connection if owner has left. |
| Transient | Retry the flow. If the failure persists after 3 retries, escalate to platform support. |

**Escalation**: If the same flow fails more than 3 times in 24 hours with
different records, escalate to Governance Administrator with run history
and correlation IDs.

---

## Runbook: Stale Sync

**Detection**: An external system record (Outlook event, Teams post, Planner
task, SharePoint file, OrgPerson cache) is out of date compared to the
Dataverse record or vice versa. IntegrationLog shows `dft_status=Queued` or
`dft_status=Retried` for more than 24 hours (detected by the
`stale-integration-status` data-quality check).

**Triage**:
1. Identify the connector and sync direction.
   - Dataverse-to-external: flow triggered but did not complete.
   - External-to-Dataverse: trigger fired but flow did not write.
2. Check the IntegrationLog for the affected record type and operation.
3. Check if the feature switchboard has the relevant capability disabled
   (`dft_FeatureDefinition`, `dft_FeatureTemplate`, `dft_MeetingFeatureOverride`).

**Response**:
| Cause | Action |
| ----- | ------ |
| Feature disabled | Enable the feature at the meeting, series, body, or definition level. No further action needed. |
| Flow not triggered | Check trigger conditions and record state. Manually trigger if the record was updated. |
| Flow running slowly | Check Power Automate analytics for throttling. Consider reducing flow complexity. |
| Connection expired | Re-authenticate connection. |
| Target system unavailable | Check Outlook, Teams, Planner, or SharePoint service health. Retry when service is restored. |

**Escalation**: If the sync has been stale for more than 72 hours and the
cause is not a disabled feature or expired connection, escalate to Power
Platform admin for flow analytics review.

---

## Runbook: Missing Files

**Detection**: A paper, minutes PDF, or transcript file is expected in
SharePoint but is not present. The Dataverse `dft_Paper`, `dft_Minutes`, or
`dft_Transcript` record references a SharePoint URL but the file cannot be
opened.

**Triage**:
1. Confirm the Dataverse record exists and has populated
   `dft_sharepointsiteid`, `dft_sharepointdriveid`, and
   `dft_sharepointitemid`.
2. Open the SharePoint URL. Classify:
   - **Not created**: File was never written (document registration flow
     did not run, or ran but failed).
   - **Deleted**: File existed but was manually or automatically deleted.
   - **Moved**: File was relocated without updating the Dataverse record.
   - **Permission**: File exists but the user cannot access it.

**Response**:
| Cause | Action |
| ----- | ------ |
| Not created | Check the document registration flow run history. Re-run after fixing any connector or permission issues. |
| Deleted | Restore from SharePoint recycle bin. Update the Dataverse record if a new file is uploaded. |
| Moved | Update the SharePoint fields on the Dataverse record with the new location. |
| Permission | Verify the user has SharePoint access to the site and library. Add if appropriate. |

**Escalation**: If a file was permanently deleted (beyond recycle bin) or
SharePoint library permissions need restructuring, escalate to SharePoint
owner with the document ID and record natural key.

---

## Runbook: Stuck Approvals

**Detection**: Minutes or decisions remain in a draft or pending-review
status for longer than the expected review period. The `dft_Minutes` or
`dft_Decision` record has a status that should have progressed.

**Triage**:
1. Check the record status and status reason.
2. Check if the assigned reviewer (`dft_approvedbyid` for minutes) has an
   active role.
3. Check if the record is caught by the `unapproved-minutes-with-publish-url`
   data-quality check (published URL but not approved).

**Response**:
| Cause | Action |
| ----- | ------ |
| Reviewer does not know approval is pending | Send manual notification. The secretary should re-notify the chair. |
| Reviewer role has changed | Update the reviewer or reassign to the new chair. Update CommitteeMembership if the chair assignment has changed. |
| Record is in an unexpected state | Manually advance the status if the review has already occurred but was not recorded. |
| Approval flow is failing | Check the approval flow run history. Fix any connector or trigger issues. |

**Escalation**: If the minutes or decision has been stuck for more than
14 days and all previous steps have been tried, escalate to Governance
Administrator for manual status override.

---

## Runbook: Unresolved Attendees

**Detection**: The `unresolved-attendees` view or metric shows attendees
linked to a meeting with no matching `dft_OrgPerson` record. The
`dft_Attendee.dft_orgpersonid` is null.

**Triage**:
1. Check if the attendee email matches an existing `dft_OrgPerson` record.
2. Check if the Office 365 Users org-hierarchy-sync flow is running and
   has successfully synced the person.
3. Check if the attendee is external (no internal directory record).

**Response**:
| Cause | Action |
| ----- | ------ |
| Office 365 Users sync not yet run | Wait for the next scheduled sync, or trigger the flow manually. |
| Attendee is external | Create a minimal `dft_OrgPerson` record with `dft_name` and `dft_workemail`. Set `dft_aadobjectid` to null. |
| Attendee email does not match directory | Verify the email is correct. If the email is a proxy address, add it as an alternative in the person record. |
| Sync flow has failed | Follow the Failed Flows runbook for the org-hierarchy-sync flow. |

**Escalation**: If the attendee is a known internal person and the Office
365 Users connector cannot find them, escalate to the identity team to
verify the person exists in the directory.

---

## Runbook: Stale External IDs

**Detection**: An IntegrationLog or data-quality check shows that a Dataverse
record has a stale or missing external ID. For example, a meeting has
`dft_outlookeventid` populated but the Outlook event was deleted and
recreated.

**Triage**:
1. Identify the affected record and the external system (Outlook, Teams,
   Planner, SharePoint).
2. Check the last sync timestamp on the Dataverse record.
3. Determine whether the external ID points to a valid resource.

**Response**:
| Cause | Action |
| ----- | ------ |
| External resource was deleted | Clear the external ID and related sync fields on the Dataverse record. The next sync flow will create a new resource and write the new ID. |
| External resource was recreated | Update the Dataverse record with the new external ID. Run the relevant sync flow to validate. |
| External ID was manually modified | Restore the correct ID from the IntegrationLog or flow run history. |

**Escalation**: If external IDs for multiple records of the same type are
stale, the sync flow or integration may have a systemic issue. Escalate to
the flow owner with affected record count and correlation IDs.

---

## Runbook: Orphaned Planner Tasks

**Detection**: The `actions-synced-without-planner-task-id` view shows
action items with `dft_plannersyncstatus=Succeeded` but
`dft_plannertaskid=null`. Alternatively, a Planner task exists but no
Dataverse action maps to it.

**Triage**:
1. Check the last planner-sync flow run for the affected action items.
2. Check if the Planner plan ID in environment variables matches the actual
   plan.
3. Check if the Planner task was deleted manually.

**Response**:
| Cause | Action |
| ----- | ------ |
| Sync flow succeeded but did not write task ID | Re-run the planner-sync flow for the affected actions. If the task ID comes back, the IntegrationLog will show it. |
| Planner task was manually deleted | Re-sync the action item (set `dft_plannersyncstatus=Queued`, clear task ID, re-run flow). |
| Planner plan reference is wrong | Update `dft_plannerplanid` in environment variables to the correct plan ID. Re-run flow for affected actions. |
| Flow reported success incorrectly | Investigate the flow logic. The sync status success condition may be too permissive. |

**Escalation**: If more than 5 action items in a single governance body are
affected, the Planner plan ownership or plan ID may be incorrect. Escalate
to the Planner plan owner.

---

## Runbook: AI Review Exceptions

**Detection**: The `ai-review-queue` view shows AI Builder runs with
completed status and pending human review. An exception occurs when:
- AI output was published without human review (data-quality error).
- AI output contains restricted-classification data despite
  `dft_classificationlimit`.
- The human reviewer finds the output is incorrect or harmful.

**Triage**:
1. Open the `dft_AIBuilderRun` record.
2. Check `dft_aibuilderpromptid` to identify which prompt was used.
3. Check `dft_humanreviewstatus` to confirm current review state.
4. Check `dft_classificationlimit` on the prompt to see the max allowed
   classification.

**Response**:
| Issue | Action |
| ----- | ------ |
| Published without review | Immediately unpublish the output. Apply the human review gate. Escalate to Governance Administrator for non-compliance review. |
| Restricted data in output | Redact the output. Review and tighten the `dft_classificationlimit` on the prompt template. Notify the data owner. |
| Output is incorrect or harmful | Reject the AI run. Direct the secretary to draft minutes manually. Log the issue in IntegrationLog for prompt improvement. |
| Reviewer did not complete review within expected time | Reassign review to another qualified reviewer. Follow up on the pending run. |

**Escalation**: Any AI output that was published without human review is an
immediate escalation to the Governance Administrator and the body chair.
Follow the AI governance controls in `docs/architecture/governance-reporting.md`.

---

## Runbook: Data-Quality Exceptions

**Detection**: The governance-audit dashboard shows data-quality check
violations. Specific checks:

- Missing owners (`recordsMissingOwnerCount`)
- Overdue reviews (`overdueReviewCount`)
- Stale integration status (`staleIntegrationStatusCount`)
- Unapproved minutes with publish URL (`unapprovedMinutesWithPublishUrlCount`)
- Actions synced without Planner task ID (`syncedActionsMissingPlannerTaskId`)

**Triage**:
1. Open the relevant view for the violated check.
2. Review the remediation guidance in `powerplatform/src/reporting/dataverse-dashboards.yaml`
   under `dataQualityChecks`.
3. Prioritise: error-severity checks (unapproved minutes with URL, actions
   without task ID) over warning-severity checks.

**Response**:
| Check | Action |
| ----- | ------ |
| Missing owners | Assign an accountable person. Update the lookup field on the record. |
| Overdue reviews | Record review outcome, update due date with approval evidence, or close the record. |
| Stale integration status | Replay, fail, or close the integration log with correlation ID evidence. |
| Unapproved minutes with publish URL | Remove the URL or complete chair approval and human review before publication. |
| Actions synced without Planner task ID | Reconcile the Planner task ID or reset sync status to retry or failed. |

**Escalation**: If the same data-quality check has been violated for more
than 30 days without remediation, escalate to the governance body secretary
and Governance Administrator for a data-quality improvement plan.

---

## Runbook: Access-Rule Errors

**Detection**: A user reports that they can see records they should not see,
or cannot see records they need for their role. The `access-exceptions-active`
view shows unexpected access rules.

**Triage**:
1. Identify the user, their Dataverse role(s), and their committee
   membership(s).
2. Check the `dft_AccessRule` records that apply to the affected records
   and the users role.
3. Check the user's Dataverse access team memberships.
4. Classify:
   - **Over-permission**: User can see records they should not based on
     their role and committee membership.
   - **Under-permission**: User cannot see records they need.

**Response**:
| Situation | Action |
| --------- | ------ |
| Over-permission due to broad role | Review the access team membership. Remove the user from the access team if their committee membership has ended. |
| Over-permission due to access rule | Update or retire the `dft_AccessRule` record. Record the reason for the change. |
| Under-permission due to missing role | Add the user to the correct Dataverse role or access team. Update CommitteeMembership if needed. |
| Under-permission due to restrictive access rule | Review and update the `dft_AccessRule` if the classification or scope is incorrect. |
| All users in a body have wrong access | Check the access team auto-provisioning configuration. The CommitteeMembership access team template may need a role-scope update. |

**Escalation**: If a user can access restricted or legal-classification
records without authorisation, escalate immediately to Governance
Administrator and the body chair. Document the incident in the IntegrationLog
with correlation ID and remediation evidence.
