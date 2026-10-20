# Decisions Workspace

Decisions Workspace is a Microsoft 365-integrated governance meeting system for committees, boards, working groups, and recurring operational forums.

The product helps users prepare, run, minute, govern, and follow through on meetings. It manages committee terms of reference, membership, agendas, papers, attendees, minutes, decisions, actions, votes, transcripts, and reporting relationships.

## Goals

- Work first in a Power Platform development environment.
- Use Microsoft-native services where possible: Dataverse, Teams, Outlook, Planner, SharePoint, Office 365 Users, AI Builder, and later Microsoft Graph.
- Keep the first version useful without Azure or Entra app registration.
- Preserve a clean upgrade path to full Dataverse, Entra, Graph, Teams meeting extensions, transcript automation, Copilot, and enterprise reporting.

## Non-Goals

- Do not clone the proprietary Decisions app.
- Do not assume tenant-wide Graph permissions for the MVP.
- Do not make ALM Accelerator, CoE Starter Kit, or Creator Kit mandatory runtime dependencies.
