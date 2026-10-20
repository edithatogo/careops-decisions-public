# Quorum Calculation

## Purpose

Define how quorum is resolved for a meeting instance in Decisions Workspace so attendees, apologies, voting eligibility, and quorum thresholds produce a deterministic outcome.

## Data Sources

Quorum depends on the following Dataverse records:

| Table | Role |
|---|---|
| `dft_TermsOfReference` | Stores quorum numerator and denominator for the governance body version in effect at the meeting date. |
| `dft_CommitteeMembership` | Active members of the governance body with voting rights. |
| `dft_Attendee` | Attendance, apology, proxy, and voting-eligibility evidence for the specific meeting. |
| `dft_Meeting` | The meeting date, status, and resolved feature switchboard. |

## Resolution Order

1. **Resolve Terms of Reference** — Find the current or meeting-effective `dft_TermsOfReference` row for the governance body linked to the meeting series. Use `dft_quorumnumber` (numerator) and `dft_quorumdenominator` (denominator choice: `TotalMembership`, `ActiveMembership`, `VotingMembership`).

2. **Resolve eligible membership** — Count active committee members at the meeting date using `dft_CommitteeMembership` where `dft_membershipstatus = 'Active'` and the meeting date falls within `dft_startdate` and `dft_enddate`.

3. **Apply denominator type**:
   - `TotalMembership`: The total count of rows in `dft_CommitteeMembership` that existed for the body at the time.
   - `ActiveMembership`: Count of members with `dft_membershipstatus = 'Active'` at meeting date.
   - `VotingMembership`: Count of members with `dft_votingrights = true` and `dft_membershipstatus = 'Active'`.

4. **Check attendance** — Count `dft_Attendee` rows for the meeting where `dft_attendancestatus = 'Present'` (or where an `IsProxy` attendee represents a voting member and is marked Present) and `dft_votingeligible = true`.

5. **Calculate present count** — Sum of Present voting-eligible attendees, including proxy representatives of voting members who are marked Present.

6. **Calculate apology count** — Count of `dft_Attendee` rows where `dft_response = 'Declined'` or `dft_attendancestatus = 'Apology'` and `dft_votingeligible = true`. Apologies do not count toward quorum but are recorded for the record.

7. **Compare to threshold** — If present count >= `dft_quorumnumber`, quorum is met. If not, quorum is not met.

## Quorum States

| State | Condition |
|---|---|
| Quorate | Present voting-eligible >= quorum numerator |
| Not Quorate | Present voting-eligible < quorum numerator |
| Not Applicable | No quorum numerator configured on the effective Terms of Reference |

## Edge Cases

- **No Terms of Reference row** — Quorum is not applicable; the meeting continues without a quorum check.
- **Zero quorum numerator** — Treated as not applicable.
- **Proxy attendees** — The proxy's attendance counts if the proxy is a voting member or the proxy is explicitly marked voting-eligible. The original member does not also count as present.
- **Multiple meetings on the same day** — Each meeting resolves quorum independently from its own attendee records.
- **Late membership changes** — Membership changes after the meeting date do not retroactively affect quorum for that meeting; the meeting-level attendee evidence is authoritative.

## MVP Implementation Notes

- Quorum is calculated by the app at meeting open or on demand, not by a flow.
- The app reads the effective Terms of Reference and attendee rows, computes the present count, and displays the quorum state.
- The quorum calculation is logged in `dft_IntegrationLog` with recordType `Quorum`.
- Future Graph attendance reports may supplement manually recorded attendee status but will not override it.

## Future Graph Path

When Microsoft Graph attendance reports become available:

1. Graph attendance data can pre-populate `dft_Attendee.attendancestatus` for eligible meetings.
2. The quorum calculation itself does not change — only the attendance input source changes.
3. Manually recorded attendance always overrides Graph-synced data for audit purposes.

## Related

- `docs/architecture/attendee-lifecycle.md` — attendee state machine and invitation flow
- `powerplatform/src/flows/outlook-invite-sync.yaml` — Outlook invite creation and attendee sync
