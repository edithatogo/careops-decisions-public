# Attendee Lifecycle

## Purpose

Document the attendee record lifecycle from invitation through attendance recording to quorum resolution, covering the MVP Outlook connector path and the future Graph sync path.

## Attendee State Machine

```
                    ┌─────────────────────────────────────┐
                    │         Invited (Pending)            │
                    │   dft_Response = null                │
                    │   dft_AttendanceStatus = Unknown     │
                    └──────────┬──────────────────────────┘
                               │
                    ┌──────────▼──────────────────────────┐
                    │         Response Recorded            │
                    │   dft_Response = {Accepted,          │
                    │        Tentative, Declined}          │
                    │   dft_ResponseUpdatedOn = set        │
                    └──────────┬──────────────────────────┘
                               │
                    ┌──────────▼──────────────────────────┐
                    │     Meeting Day / Manual Entry       │
                    │   dft_AttendanceStatus = {Present,   │
                    │        Absent, Apology, Late}        │
                    └──────────┬──────────────────────────┘
                               │
                    ┌──────────▼──────────────────────────┐
                    │        Attendance Finalised          │
                    │   Used in quorum calculation         │
                    │   Recorded in meeting minutes        │
                    └─────────────────────────────────────┘
```

## Attendee Record Structure

Each attendee row in `dft_Attendee` holds:

| Field | Purpose |
|---|---|
| `dft_MeetingId` | Link to the meeting instance |
| `dft_OrgPersonId` | Resolved person (nullable for external invitees) |
| `dft_DisplayName` | Display name (always populated) |
| `dft_EmailAddress` | Email for invite routing |
| `dft_Response` | Accepted, Tentative, Declined, or null |
| `dft_AttendanceStatus` | Present, Absent, Apology, Late, Unknown |
| `dft_AttendeeRole` | Required, Optional, Observer, Chair, Secretariat |
| `dft_IsProxy` | True if this person is attending on behalf of another |
| `dft_VotingEligible` | True if this attendee can vote on motions |
| `dft_InvitedOn` | When the Outlook invite was sent |
| `dft_ResponseUpdatedOn` | Last response update from Outlook |
| `dft_OutlookAttendeeId` | External Outlook attendee identifier |

## Record Lifecycle

### 1. Invitation (MVP: Outlook Connector Path)

1. Secretariat creates or updates a meeting in the app.
2. The `outlook-invite-sync` flow is triggered when `dft_InviteRequested = true` and `dft_InviteSyncState ≠ Succeeded`.
3. The flow resolves required and optional attendees from the meeting row.
4. An Outlook calendar event is created or updated with the resolved attendee list.
5. The Outlook event ID is stored back on `dft_Meeting.dft_OutlookEventId`.
6. Attendee rows are created or updated in `dft_Attendee` with `dft_Response = null` and `dft_AttendanceStatus = Unknown`.

### 2. Response (MVP: Manual or Outlook)

- In the MVP, attendee responses can be recorded manually by the secretariat.
- As Outlook responses arrive, they can be manually transcribed into `dft_Response` and `dft_ResponseUpdatedOn`.
- Future Graph sync would import responses automatically.

### 3. Meeting Day (Manual Recording)

1. During or after the meeting, the secretariat records each attendee's `dft_AttendanceStatus`.
2. The chair or secretariat records apologies for known absentees.
3. Proxy relationships are captured: a person attending as proxy for a voting member is recorded with `dft_IsProxy = true`.
4. Voting eligibility is confirmed: `dft_VotingEligible` may differ from membership `dft_votingrights` if a member has a declared conflict.

### 4. Quorum Resolution

Once attendance is recorded:
1. The app resolves the current quorum threshold from the governance body's active Terms of Reference.
2. Present voting-eligible attendees are counted.
3. Quorum status is displayed on the meeting record.
4. See `docs/architecture/quorum-calculation.md` for detailed logic.

### 5. Minutes and Audit

- Attendance and apology lists are included in draft and final minutes.
- The attendee record is preserved as-is after the meeting is closed.
- Changes after approval are tracked in audit history.

## Apology Records

An apology is recorded when:
- A member notifies the secretariat they cannot attend.
- The `dft_AttendanceStatus` is set to `Apology`.
- The apology is noted in minutes but does not count toward quorum.

Apologies are stored as `dft_Attendee` rows with `dft_AttendanceStatus = 'Apology'` and `dft_Response = 'Declined'`.

## Future Graph Sync Path

When Graph calendar and Teams meeting APIs become available:

1. **Attendee creation**: Attendees are created from the Graph event attendees rather than the Outlook connector.
2. **Response sync**: `dft_Response` is updated automatically from Graph presence data.
3. **Attendance reports**: Graph Teams attendance reports pre-populate `dft_AttendanceStatus` for meetings with Teams meetings enabled.
4. **Override**: Manual attendance recording always takes priority over Graph-synced data.

## Related

- `docs/architecture/quorum-calculation.md` — quorum calculation with attendee and apology data
- `powerplatform/src/flows/outlook-invite-sync.yaml` — Outlook invite creation and attendee sync contract
- `docs/architecture/integration-map.md` — meeting lifecycle and integration summary
