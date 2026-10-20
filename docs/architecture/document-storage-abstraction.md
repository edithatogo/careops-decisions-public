# Document Storage Abstraction

## Purpose

Decisions Workspace stores documents in Dataverse-first mode during the MVP, with a clear migration path to SharePoint document libraries when DMS integration is available. The schema supports both without changing user-facing concepts.

## Dataverse-First Path (MVP)

In the MVP, documents (papers, minutes, transcript files) are stored using Dataverse's built-in file and note capabilities:

- **Papers**: Document metadata stored on `dft_Paper`; file content optionally stored in Dataverse file columns or as notes.
- **Minutes**: Minutes text stored in `dft_drafttext` column; published versions stored in Dataverse.
- **SharePoint columns empty**: All `dft_sharepoint*` fields remain null until SharePoint integration is active.

### Advantages for MVP

- No dependency on SharePoint connector or site provisioning.
- Works within Dataverse for Teams and Dataverse Standard.
- Data is always available in the app without cross-system lookups.
- Simplifies backup and export during the build phase.

### Limitations

- No version history on file content (Dataverse file columns have basic versioning).
- No integration with SharePoint retention, DLP, or co-authoring.
- Limited to Dataverse storage quotas.

## SharePoint Path (Future)

When SharePoint integration is activated:

1. Provision SharePoint document libraries per governance body or meeting series.
2. Migrate existing document content from Dataverse to SharePoint.
3. Populate `dft_sharepointsiteid`, `dft_sharepointdriveid`, `dft_sharepointitemid` on Paper, Minutes, and Transcript records.
4. Use SharePoint connector actions in Power Automate for upload, version management, and permissions.

### Migration Trigger

Migration from Dataverse-first to SharePoint storage should be activated when:
- SharePoint connector is available in the target environment.
- Document libraries are provisioned for the relevant governance bodies.
- A service account or user context with write permissions is configured.

### Fields That Support Storage Abstraction

| Table | Column | Purpose | Dataverse-First | SharePoint |
| --- | --- | --- | --- | --- |
| Paper | `dft_sharepointsiteid` | SharePoint site GUID | null | populated |
| Paper | `dft_sharepointdriveid` | Document library GUID | null | populated |
| Paper | `dft_sharepointitemid` | File item GUID | null | populated |
| Paper | `dft_sharepointurl` | Direct file URL | null | populated |
| Paper | `dft_documentid` | External document identifier | used as key | used as key |
| Paper | `dft_documentsource` | Origin of the document | dataverse_file | sharepoint |
| Minutes | `dft_sharepoint*` | Same pattern as Paper | null | populated |
| Minutes | `dft_publishedurl` | URL to published document | Dataverse URL | SharePoint URL |
| Transcript | `dft_sharepoint*` | Same pattern as Paper | null | populated |
| Transcript | `dft_fileurl` | Source file URL | Dataverse URL | SharePoint URL |

### Resolution Logic

When the app needs to access a document:
1. If `dft_sharepointurl` is populated → use SharePoint.
2. If `dft_documentsource = 'dataverse_file'` → use Dataverse file column or note.
3. If neither → document metadata exists but file content has not been uploaded.

This logic keeps the app working during migration: some documents may be in SharePoint while others remain in Dataverse.
