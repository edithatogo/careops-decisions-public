# Tenant Permissions Matrix

| Role Or Permission | Required For | MVP Gate | Future Gate |
| --- | --- | --- | --- |
| Power Platform maker | Create apps, flows, tables, solution artifacts | Required | Required |
| Environment admin | Environment settings, DLP visibility, solution import/export | Preferred for live build | Required for shared test/prod |
| Dataverse system customizer/admin | Tables, choices, relationships, roles | Required for schema build | Required |
| Connection owner | Outlook, Teams, Planner, SharePoint, Office 365 Users connectors | Required for active flows | Required |
| Teams owner | App installation, channel notifications, tab setup | Required for Teams app MVP | Required |
| SharePoint owner | Document library setup and permissions | Required for papers/minutes storage | Required |
| Planner plan owner | Action sync and task ownership | Required if Planner sync enabled | Required |
| AI Builder admin/capacity owner | Prompt use and capacity allocation | Required if AI enabled | Required |
| Graph admin consent | Transcripts, attendance, richer directory sync | Not required | Required |
| App registration admin | Future Graph/Azure integrations | Not required | Required |
| Azure RBAC owner/contributor | Azure-hosted integration services | Not required | Required |
| Entra group admin | Group-based security alignment | Not required | Required |
