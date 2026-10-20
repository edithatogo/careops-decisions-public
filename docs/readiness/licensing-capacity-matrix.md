# Licensing And Capacity Matrix

Status key: **C** = Confirmed (licensing verified), **A** = Assumed (not yet verified with tenant/licensing owner).

| Capability | Status | MVP Need | Later Need | Gate |
| --- | --- | --- | --- | --- |
| Dataverse or Dataverse for Teams | A | Required for canonical records | Full Dataverse for enterprise security and ALM | Confirm environment type before live build |
| Power Apps | A | Required for Teams/canvas workflow and model-driven admin | Same, with managed deployment | Confirm maker and app user licensing |
| Power Automate | A | Required for Outlook, Teams, Planner, SharePoint, Office 365 Users flows | Premium/custom connectors if needed | Confirm connector licensing and DLP |
| AI Builder | A | Optional for MVP; required for automated transcript-to-minutes | Capacity planning and prompt governance | Confirm credits/capacity before enabling |
| Planner | A | Required for action sync if used | Enterprise task sync governance | Confirm Planner plan ownership |
| Teams | A | Required for app hosting and notifications | Meeting extension and richer Teams integration | Confirm team/channel ownership |
| Outlook | A | Required for invite sync | Graph calendar sync later | Confirm mailbox/connection ownership |
| SharePoint | A | Required for papers, agendas, minutes, transcripts, and packs | Retention/sensitivity labels later | Confirm library ownership and permissions |
| Power BI/Fabric | A | Deferred | Enterprise reporting and semantic model | Implement only after stable Dataverse data |
| Microsoft Graph | A | Deferred | Transcripts, attendance, group/user sync | Requires app registration and admin consent |
| Azure | A | Deferred | Integration services, managed identities, monitoring | Requires subscription and RBAC |
| Entra | A | Deferred | Groups, app registrations, service principals | Requires tenant permissions |

## Confirmation Notes

- All licenses are currently **Assumed** because no tenant access or
  licensing review has been completed.
- Confirm each capability with the Power Platform admin or licensing owner
  before enabling in a shared environment.
- The following are included in common Power Platform per-user license
  bundles and are likely confirmed on review: Dataverse for Teams, Power
  Apps, Power Automate, Planner, Teams, Outlook.
- The following typically require additional purchase or capacity
  allocation and are less likely to be confirmed without approval: AI
  Builder credits, Power BI/Fabric capacity, Premium connector licenses.
- The following require tenant-level admin consent and are deferred:
  Graph, Azure, Entra.
