# Lightweight Threat Model

## Scope

This threat model covers the Decisions Workspace MVP deployment on
Dataverse for Teams or full Dataverse, with Power Automate flows touching
Outlook, Teams, Planner, SharePoint, Office 365 Users, and AI Builder.
Future Graph, Azure, and Entra integrations are noted but not detailed.

## Methodology

STRIDE-lite per asset.

---

## Asset: Dataverse Records (All Tables)

| Threat | STRIDE | Likelihood | Impact | Mitigation |
| ------ | ------ | ---------- | ------ | ---------- |
| Unauthorised user reads restricted records | Info Disclosure | Medium | High | Security roles, access teams, access rules enforced by Dataverse. No by-pass for non-admin. |
| Unauthorised user creates or modifies records | Tampering | Low | High | CRUD privileges scoped by security role matrix. Only Secretary and Administrator can create meetings, items, and minutes. |
| Former member retains access | Elevation of Privilege | Medium | Medium | CommitteeMembership end date terminates access team membership. Manual offboarding required if end date not set. |
| Internal user misuses elevated privileges | Elevation of Privilege | Low | Medium | Separate Secretary and Administrator roles. Audit logging via `dft_IntegrationLog` and Dataverse audit. |
| Access rule bypass via direct table access | Info Disclosure | Low | Medium | Dataverse security is table-level; access rules are design records, not enforcement. Manual sharing or customisation could bypass intended restriction. |

## Asset: Power Automate Flows

| Threat | STRIDE | Likelihood | Impact | Mitigation |
| ------ | ------ | ---------- | ------ | ---------- |
| Flow run with stale or revoked connection | Spoofing | Low | High | Connection owner owns the credential. If owner leaves, flows fail until connection is updated. Service account recommended for production. |
| Flow writes incorrect data due to schema change | Tampering | Medium | Medium | Flow contracts document expected schema. Schema changes require contract update. Solution checker catches some reference breaks. |
| Flow exposes sensitive data in error message | Info Disclosure | Medium | Low | IntegrationLog stores error messages. Ensure no personal data or AI output is logged in error messages without redaction. |
| Trigger fires on unexpected record change | Denial of Service | Low | Low | All flows use trigger conditions. Rate limits in Power Automate prevent runaway execution. |

## Asset: AI Builder Prompts And Runs

| Threat | STRIDE | Likelihood | Impact | Mitigation |
| ------ | ------ | ---------- | ------ | ---------- |
| AI prompt generates incorrect or harmful output | Tampering | Medium | High | Human review gate (ADR 0004) requires review before publication. Prompt templates are versioned and approved. |
| AI output contains personal or restricted data | Info Disclosure | Medium | High | AIBuilderPrompt has `dft_classificationlimit` to cap input sensitivity. Restricted and Legal classifications should not be sent to AI Builder. Review governance covers output before publication. |
| Unauthorised prompt modification | Tampering | Low | High | Only Administrator can modify prompt templates. AIBuilderPrompt has approval status workflow. |

## Asset: Connector Access (Outlook, Teams, Planner, SharePoint)

| Threat | STRIDE | Likelihood | Impact | Mitigation |
| ------ | ------ | ---------- | ------ | ---------- |
| Connector sends unauthorised email or post | Spoofing | Low | High | Flow scopes limit actions to governance meeting context. No bulk or broadcast capability. DLP restricts connector combinations. |
| Connector reads data outside governance scope | Info Disclosure | Low | Medium | SharePoint connector scoped to specific site/library IDs from environment variables. Outlook connector sends to specific meeting attendees only. |
| Connector credential compromised | Tampering | Low | High | Use service accounts with minimal permissions where possible. Rotate connection references periodically. Monitor IntegrationLog for unexpected activity. |

## Asset: External Sharing (Packs, PDFs, SharePoint External)

| Threat | STRIDE | Likelihood | Impact | Mitigation |
| ------ | ------ | ---------- | ------ | ---------- |
| Exported meeting pack shared with wrong recipient | Info Disclosure | Medium | High | External packs are watermarked. Use SharePoint external sharing with expiration. Log each pack export in IntegrationLog. |
| PDF password shared insecurely | Info Disclosure | Medium | Medium | Send password via separate channel (phone or in-person). Record password distribution method in meeting notes. |
| External SharePoint link forwarded without authorisation | Info Disclosure | Medium | Medium | SharePoint external sharing links default to specific people with expiration. View-only permission. |

## Asset: Oversharing Configuration

| Threat | STRIDE | Likelihood | Impact | Mitigation |
| ------ | ------ | ---------- | ------ | ---------- |
| Feature switchboard enables unintended capability | Tampering | Low | Medium | FeatureTemplate and MeetingFeatureOverride require Administrator or Secretary write. Default states are conservative (disabled where unsure). |
| Per-meeting override increases access scope without review | Elevation of Privilege | Low | Medium | MeetingFeatureOverride records the reason. Access exception review (governance-audit dashboard) surfaces changes for review. |

## Asset: Privileged Maker Access

| Threat | STRIDE | Likelihood | Impact | Mitigation |
| ------ | ------ | ---------- | ------ | ---------- |
| Maker modifies app or flow to bypass security rules | Elevation of Privilege | Low | High | Dataverse roles prevent makers from reading or writing records unless the maker also has a governance role. Separate maker environment for development. Solution-managed deployment prevents unauthorised customisation in production. |
| Maker exports solution containing sensitive data | Info Disclosure | Low | Medium | Remove sample data before solution export. Use managed solutions for production deployment. |

## Asset: Stale Permissions

| Threat | STRIDE | Likelihood | Impact | Mitigation |
| ------ | ------ | ---------- | ------ | ---------- |
| Member role persists after committee membership ends | Elevation of Privilege | Medium | Medium | Access team membership is linked to CommitteeMembership status. Deactivate membership record to remove access. Run quarterly audit of active vs. expected members. |
| Service account credentials still valid after decommission | Elevation of Privilege | Low | Medium | Document service account ownership. Include in quarterly access review. Disable or rotate credentials on owner departure. |

## Risk Acceptance

The following risks are accepted for MVP:

1. **Manual external pack security**: Exported meeting packs rely on
   SharePoint external sharing settings and manual password distribution.
   Automated DRM or information protection is deferred to the enterprise
   path.
2. **No automated retention enforcement**: Retention periods are documented
   but not automated. Purge and archival are manual processes until full
   Dataverse or Microsoft Purview retention policies are available.
3. **No automated access rule enforcement**: `dft_AccessRule` records
   intended access but is not an enforcement engine. Full Dataverse
   security roles and Entra groups are required to enforce the rules
   described in the access rules.
4. **Stale permission detection is manual**: No automated scanner
   identifies orphaned access team memberships, expired guest accounts, or
   inactive service accounts. The governance-audit dashboard surfaces
   access-exception records for manual review.

## Review Cadence

- Threat model review: quarterly, or on any significant architecture change.
- Access review: quarterly, covering service accounts, guest accounts,
  Administrator role assignments, and access-exception records.
- DLP review: on each new connector or flow addition.
