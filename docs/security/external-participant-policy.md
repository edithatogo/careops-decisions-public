# External Participant Policy

## Definition

An external participant is any person who is not a member, employee, or
authorised representative of the organisation that owns the governance body.
External participants include:

- Guest presenters invited to present on a specific agenda item.
- External auditors or regulators attending for compliance review.
- Observers from partner organisations with a limited-scope memorandum of
  understanding.
- Members of the public attending open portions of a meeting (rare).

## Default Restriction

**By default, all records in Decisions Workspace are internal-only.** An
external participant must not see any record unless an explicit access rule
grants them access. The `dft_AccessRule` table records these grants.

## Roles Available To External Participants

| Role | Available To External | Notes |
| ---- | -------------------- | ----- |
| Guest Presenter | Yes | Time-boxed; meeting-scope read access only |
| Restricted Reader | Yes | Explicitly scoped via access rule |
| Auditor | No (unless regulatory) | Internal organisation audit role only |
| Member | No | Requires governance body membership |
| Chair | No | Organisation employee only |
| Secretary | No | Organisation employee only |
| Administrator | No | Organisation employee only |
| Author | No | Organisation employee only |
| Service Account | No | Internal automated account only |

## Guest Presenter Terms

1. Access is scoped to a single meeting and its associated agenda items,
   papers, and attendee list.
2. Access duration begins 7 days before the meeting and ends 30 days after
   the meeting end date.
3. Guest Presenter must not access:
   - Minutes, decisions, or actions from other meetings.
   - Transcripts.
   - Conflict of interest records.
   - Access rules.
   - Integration logs.
   - AI Builder runs.
   - Member contact details outside the meeting scope.
4. The secretary records the guest presenters intent, scope, and duration
   in the `dft_Meeting.dft_requiredattendees` or
   `dft_Meeting.dft_optionalattendees` field with a `[Guest]` prefix.
5. A corresponding `dft_AccessRule` with `dft_ruletype=Allow`,
   `dft_scope=meeting_only`, and `dft_classification=Confidential` (or
   lower) is created for each guest presenter engagement.

## External Participant Data Handling

1. External participants sign in with their own identity provider or a
   Microsoft Entra B2B guest account, not shared credentials.
2. External participants do not receive Dataverse access directly.
   Meeting packs are exported and shared via SharePoint external sharing
   with expiration, or delivered as password-protected PDFs.
3. If external access through Dataverse is required, a dedicated
   Restricted Reader security role must be assigned to the B2B guest
   account with an expiration date.
4. All exported packs shared with external participants must be watermarked
   or branded "External - Confidential".

## Audit Trail

The secretary must record for each external participant engagement:

- Participant name and organisation.
- Purpose of participation.
- Meeting and agenda item(s) covered.
- Access scope (Dataverse or exported packs).
- Access start and end date.
- Records shared (agenda, papers, minutes extract only).
- Secretary who authorised the access.

These records are maintained in the Dataverse `dft_AccessRule` and
`dft_Meeting.dft_requiredattendees` / `dft_optionalattendees` fields.
