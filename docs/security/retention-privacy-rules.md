# Retention And Privacy Rules

## Record Classification Definitions

Records are classified using the `recordClassification` choice set:

- **Public**: Open to all stakeholders and the public after publication.
- **Internal**: Visible to all authenticated members of the governing
  organisation. Includes routine administrative records.
- **Confidential**: Limited to the governance body, secretariat, and
  officers. Includes draft minutes, papers, and decisions under review.
- **Restricted**: Limited to explicitly named individuals or roles.
  Includes legal advice, personnel matters, and commercially sensitive
  content.
- **Legal**: Subject to legal professional privilege or statutory secrecy.
  Access requires explicit approval from the governance body or legal
  counsel.

## Retention Rules By Record Type

Unless overridden by organisation-specific policy, apply these defaults.

### Governance Body Records

| Record Type | Classification Default | Minimum Retention | Disposal Action |
| ----------- | --------------------- | ---------------- | --------------- |
| `dft_GovernanceBody` | Internal | Indefinite (permanent register) | Archive status only |
| `dft_TermsOfReference` | Internal | Current + 10 years after superseded | Archive or retain as historical record |
| `dft_CommitteeMembership` | Internal | Current membership + 7 years after end date | Archive or confidential delete |
| `dft_OrgPerson` | Internal | 7 years after last active membership | Anonymise or delete |

### Meeting Records

| Record Type | Classification Default | Minimum Retention | Disposal Action |
| ----------- | --------------------- | ---------------- | --------------- |
| `dft_MeetingSeries` | Internal | Indefinite (series register) | Archive status only |
| `dft_Meeting` | Internal | Current + 10 years after meeting date | Archive or close status |
| `dft_MeetingFeatureOverride` | Internal | Same as parent meeting | Cascade with meeting |
| `dft_Attendee` | Internal | 7 years after meeting date | Anonymise or aggregate |
| `dft_AgendaItem` | Internal | Same as parent meeting | Cascade with meeting |

### Document Records

| Record Type | Classification Default | Minimum Retention | Disposal Action |
| ----------- | --------------------- | ---------------- | --------------- |
| `dft_Paper` | Confidential | 7 years after meeting date | Archive or confidential delete; retain metadata |
| `dft_Minutes` | Confidential | Current + 10 years after approval | Approved: archive; draft: delete after 3 years |
| `dft_Transcript` | Confidential | Redact within 90 days if not retained for audit purpose | Redact or delete per transcript status; retain metadata |
| `dft_Decision` | Internal | Indefinite (decision register) | Archive or rescind status only |
| `dft_ActionItem` | Internal | 7 years after completion | Archive or close; anonymise owner on disposal |
| `dft_Vote` | Internal | 7 years after meeting date | Archive with minutes |
| `dft_VoteResponse` | Confidential | Same as parent vote | Cascade with vote |
| `dft_ConflictOfInterest` | Confidential | 10 years after declaration | Archive; confidential retention |

### AI And Integration Records

| Record Type | Classification Default | Minimum Retention | Disposal Action |
| ----------- | --------------------- | ---------------- | --------------- |
| `dft_AIBuilderPrompt` | Internal | Indefinite (prompt register) | Archive or retire status only |
| `dft_AIBuilderRun` | Confidential | 3 years after run | Delete output text; retain metadata for audit |
| `dft_IntegrationLog` | Internal | 1 year | Delete after 1 year; retain aggregate metrics |
| `dft_AccessRule` | Internal | Indefinite (audit register) | Archive or supersede status only |
| `dft_FeatureDefinition` | Internal | Indefinite (switchboard register) | Archive or disable status only |
| `dft_FeatureTemplate` | Internal | Same as parent governance body or series | Cascade with parent |

## Privacy Rules

### Personal Data

The following Dataverse columns contain personal data and must be treated
in accordance with applicable privacy regulations:

| Table | Columns | Sensitivity | Handling |
| ----- | ------- | ----------- | -------- |
| `dft_OrgPerson` | `dft_name`, `dft_workemail`, `dft_userprincipalname`, `dft_jobtitle`, `dft_department`, `dft_manageremail`, `dft_aadobjectid` | High | Mask in reports; export only with legitimate interest; delete on membership end + 7 years |
| `dft_Attendee` | `dft_displayname`, `dft_emailaddress` | Medium | Retain for meeting quorum evidence; anonymise after 7 years |
| `dft_CommitteeMembership` | (references `dft_OrgPerson`) | Medium | Retain for governance audit trail |
| `dft_ActionItem` | `dft_ownerid` (references `dft_OrgPerson`) | Medium | Retain for accountability; anonymise on disposal |
| `dft_VoteResponse` | `dft_orgpersonid` (references `dft_OrgPerson`) | High | Retain for vote audit; never expose outside governance body |
| `dft_ConflictOfInterest` | `dft_orgpersonid` (references `dft_OrgPerson`), `dft_disclosuretext` | High | Restricted access; retain for compliance period |

### Privacy Principles

1. **Purpose limitation**: Personal data is collected and retained only for
   governance meeting administration, decision audit, and compliance purposes.
2. **Data minimisation**: Store the minimum personal data needed for the
   governance function. Do not import additional HR or directory fields
   without documented need.
3. **Access limitation**: Personal data access is scoped by security role,
   governance body membership, and access rule. No user sees personal data
   outside their governance scope.
4. **Retention limitation**: Delete or anonymise personal data when the
   retention period expires. Use the `dft_retentionstatus` column on
   `dft_Transcript` as a pattern for other tables.
5. **Subject access**: Export personal data for a data subject upon verified
   request by using Dataverse view filters by `dft_workemail` or
   `dft_orgpersonid`.

## Implementation Notes

- Retention rules are **design references**, not automated policies. No
  Dataverse retention or purge flow is implemented for MVP.
- Dataverse for Teams has limited retention tooling. Full Dataverse
  provides hold and retention policies.
- Future SharePoint sensitivity labels and Microsoft Purview retention
  labels can be mapped to the classification values above.
- The `dft_retentionstatus` choice on `dft_Transcript` provides a pattern
  that other tables can adopt when automated retention is implemented.
