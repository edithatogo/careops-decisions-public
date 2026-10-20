# Document Storage Conventions

This document defines how documents are stored, named, versioned, and retained for the Decisions workspace.

## Document Types

| Type | Storage Location | Path Convention |
| --- | --- | --- |
| Agendas | SharePoint document library / Teams files | `{Site}/Decisions/{BodyName}/{MeetingDate}/Agenda/` |
| Papers | SharePoint document library | `{Site}/Decisions/{BodyName}/{MeetingDate}/Papers/` |
| Minutes (draft) | SharePoint document library | `{Site}/Decisions/{BodyName}/{MeetingDate}/Minutes/` |
| Minutes (approved) | SharePoint document library | `{Site}/Decisions/{BodyName}/{MeetingDate}/Minutes/` |
| Transcripts | SharePoint document library | `{Site}/Decisions/{BodyName}/{MeetingDate}/Transcripts/` |
| Restricted packs | Separate restricted library | `{Site}/Decisions-Restricted/{BodyName}/{MeetingDate}/` |
| Published decisions | SharePoint document library | `{Site}/Decisions/Register/` |

## Naming Conventions

- Files: `{MeetingDate}_{ItemSequence}_{DescriptiveName}.{ext}`
  - Example: `2026-05-10_01_Chairpersons-Report.docx`
- Restricted files: prefix with `RESTRICTED-`
  - Example: `2026-05-10_RESTRICTED_Confidential-Paper.docx`
- Folder names use `YYYY-MM-DD` date format

## Versioning

- All documents use SharePoint versioning (major versions on save/check-in)
- Minutes versions: draft -> v1.0 (review submitted) -> v2.0 (reviewer edits) -> v3.0 (approved)
- Published decision register entries are final and not overwritten

## Retention

| Document Type | Retention | Disposition |
| --- | --- | --- |
| Agendas | 3 years after meeting | Archive |
| Papers | 3 years after meeting | Archive |
| Minutes (approved) | Permanent | Transfer to records management |
| Transcripts | 1 year | Delete after minutes approved |
| AI prompt evidence | Duration of solution | Retain with audit records |
| Published decisions | Permanent | Transfer to records management |

## Classification

| Document Type | Sensitivity Label |
| --- | --- |
| Published agendas and minutes | Official / Public |
| Restricted papers | Confidential |
| Unapproved drafts | Internal |
| AI Builder inputs/outputs | Internal (with retention) |

## Dataverse-to-Document Library Mapping

Dataverse records map to document library metadata columns:

| Metadata Column | Source |
| --- | --- |
| `decisions_BodyName` | Governing body name from Dataverse |
| `decisions_MeetingDate` | Meeting date from Dataverse meeting record |
| `decisions_AgendaItemId` | Related agenda item GUID |
| `decisions_DocumentType` | Agendas, Papers, Minutes, Transcript, Register |
| `decisions_Classification` | Sensitivity label value |
| `decisions_RetentionDate` | Calculated from retention rule above |

## Current Status

SharePoint document library paths are placeholders until the SharePoint connector is available in the target environment. The path conventions are documented here for implementation once connector access is enabled.
