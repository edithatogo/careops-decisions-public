# Import Mapping Templates

## Purpose

These templates describe how source data fields map to Dataverse columns for each register type. Use them when importing from existing registers (e.g. Excel, SharePoint lists, or NSW Health committee systems).

## Committee Register (→ dft_GovernanceBody)

| Source Field | Source Example | Target Column | Required | Transformation |
| --- | --- | --- | --- | --- |
| Committee Name | "Medication Safety Committee" | `dft_name` | yes | Truncate to 200 chars |
| Body Code | "medication-safety-committee" | `dft_bodycode` | yes | Lowercase, hyphenate; unique |
| Body Type | "committee" | `dft_bodytype` | yes | Must match choice value |
| Parent Body | "clinical-governance-council" | `dft_parentgovernancebodyid` | no | Lookup by body code |
| Status | "Active" | `dft_status` | yes | Lowercase to match choice |
| Description | "Governs medication safety..." | `dft_description` | no | Max 2000 chars |
| Default Classification | "internal" | `dft_defaultclassification` | no | Must match choice value |

## Minutes Register (→ dft_Minutes)

| Source Field | Source Example | Target Column | Required | Transformation |
| --- | --- | --- | --- | --- |
| Meeting | "meeting-medication-safety-2026-05-20" | `dft_meetingid` | yes | Lookup by meeting alternate key |
| Title | "Medication Safety Committee Minutes - 20 May 2026" | `dft_name` | yes | Truncate to 200 chars |
| Version | 1 | `dft_versionnumber` | yes | Integer |
| Status | "draft" | `dft_status` | yes | Must match choice value |
| Draft Text | "...full minutes text..." | `dft_drafttext` | no | Max 32000 chars |
| Approved By | "chair@example.invalid" | `dft_approvedbyid` | no | Lookup by person work email |
| Approved On | "2026-05-20T09:00:00Z" | `dft_approvedon` | no | ISO 8601 datetime |
| Published On | "2026-05-20T10:00:00Z" | `dft_publishedon` | no | ISO 8601 datetime |
| Published URL | "https://..." | `dft_publishedurl` | no | Max 500 chars |
| Classification | "internal" | `dft_classification` | yes | Must match choice value |

## Paper Register (→ dft_Paper)

| Source Field | Source Example | Target Column | Required | Transformation |
| --- | --- | --- | --- | --- |
| Meeting | "meeting-medication-safety-2026-05-20" | `dft_meetingid` | yes | Lookup by meeting alternate key |
| Agenda Item | "msc-agenda-03" | `dft_agendaitemid` | no | Lookup by agenda item name |
| Title | "Medication Incident Trend Summary" | `dft_name` | yes | Truncate to 200 chars |
| Document ID | "paper-msc-incident-trend-2026-05" | `dft_documentid` | no | Used as natural key |
| Document Source | "sharepoint" | `dft_documentsource` | yes | Must match choice value |
| Classification | "internal" | `dft_classification` | yes | Must match choice value |
| Status | "circulated" | `dft_paperstatus` | yes | Must match choice value |
| Owner | "owner@example.invalid" | `dft_ownerid` | no | Lookup by person work email |
| SharePoint URL | "https://..." | `dft_sharepointurl` | no | Max 500 chars |

## Decision Log (→ dft_Decision)

| Source Field | Source Example | Target Column | Required | Transformation |
| --- | --- | --- | --- | --- |
| Meeting | "meeting-medication-safety-2026-05-20" | `dft_meetingid` | yes | Lookup by meeting alternate key |
| Agenda Item | "msc-agenda-04" | `dft_agendaitemid` | no | Lookup by agenda item name |
| Governance Body | "gb-medication-safety-governance" | `dft_governancebodyid` | no | Lookup by body code |
| Decision Number | "MSC-DEC-2026-05-001" | `dft_decisionnumber` | no | Used as natural key |
| Title | "Endorse high-risk medicines action plan" | `dft_name` | yes | Truncate to 200 chars |
| Decision Text | "The committee endorsed..." | `dft_decisiontext` | yes | Max 4000 chars |
| Authority | "Medication Safety Committee ToR" | `dft_authority` | no | Max 200 chars |
| Status | "resolved" | `dft_status` | yes | Must match choice value |
| Effective On | "2026-05-20" | `dft_effectiveon` | no | Date only |
| Review Due On | "2026-08-20" | `dft_reviewdueon` | no | Date only |
| Owner | "owner@example.invalid" | `dft_ownerid` | no | Lookup by person work email |

## Action Log (→ dft_ActionItem)

| Source Field | Source Example | Target Column | Required | Transformation |
| --- | --- | --- | --- | --- |
| Meeting | "meeting-medication-safety-2026-05-20" | `dft_meetingid` | yes | Lookup by meeting alternate key |
| Decision | "decision-msc-2026-05-001" | `dft_decisionid` | no | Lookup by decision number |
| Governance Body | "gb-medication-safety-governance" | `dft_governancebodyid` | no | Lookup by body code |
| Agenda Item | "msc-agenda-03" | `dft_agendaitemid` | no | Lookup by agenda item name |
| Action Number | "MSC-ACT-2026-05-001" | `dft_actionnumber` | no | Used as natural key |
| Title | "Update incident trend summary" | `dft_name` | yes | Truncate to 200 chars |
| Summary | "Quality lead to update..." | `dft_actionsummary` | yes | Max 4000 chars |
| Owner | "owner@example.invalid" | `dft_ownerid` | yes | Lookup by person work email |
| Due Date | "2026-05-15" | `dft_duedate` | no | Date only |
| Status | "open" | `dft_status` | yes | Must match choice value |
| Priority | "high" | `dft_priority` | yes | Must match choice value |
| Sync to Planner | true | `dft_synctoplanner` | no | Boolean |
| Completion Evidence | "sharepoint://..." | `dft_completionevidence` | no | Max 4000 chars |

## Notes

- Lookup fields reference existing records by their natural keys (body code, work email, meeting alternate key, etc.).
- Choice values must match the choice code (lowercase) defined in `choices.yaml`, not the display label.
- All source data is treated as synthetic during import. Real NSW Health data remains gated to authorised source systems.
