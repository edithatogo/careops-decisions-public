# ADR 0010: Document Storage Strategy

## Status

Accepted.

## Context

Meeting papers, minutes PDFs, transcripts, and meeting packs are documents
that need durable storage, version history, and access control. Dataverse
can store file data but has size and collaboration limitations. SharePoint
provides richer document management but adds connector dependencies. The
MVP needs a storage strategy that works for Dataverse for Teams and scales
to full Dataverse with SharePoint.

## Decision

Use SharePoint as the document repository and Dataverse as the document
register. The Dataverse tables (`dft_Paper`, `dft_Minutes`, `dft_Transcript`)
store SharePoint site, drive, and item IDs for each document. The actual
file content lives in SharePoint document libraries.

Key rules:

1. **Dataverse is authoritative for metadata.** The document title,
   classification, owner, status, and source are Dataverse columns.
2. **SharePoint is authoritative for file content.** Version history,
   check-in/check-out, co-authoring, and file-level permissions use
   SharePoint.
3. **Document registration flows** create the SharePoint file and write
   the SharePoint IDs back to the Dataverse record.
4. **For Dataverse for Teams** (no SharePoint connector), documents are
   stored as Dataverse file columns or as external links. SharePoint
   integration is activated when the SharePoint connector is available.

## Consequences

- SharePoint document libraries must exist before document registration
  flows can run.
- Dataverse records can exist without SharePoint files (paper status may
  be Draft even without a file).
- Document version history is in SharePoint; the Dataverse record tracks
  the current version only.
- SharePoint file-level permissions should mirror Dataverse access rules
  in the enterprise path, but this is deferred.
- Meeting pack assembly requires reading both Dataverse records and
  SharePoint files.
