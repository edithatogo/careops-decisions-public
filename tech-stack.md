# Tech Stack

## Short-Term Platform

- Power Apps canvas app embedded in Teams.
- Dataverse for Teams or Dataverse, depending on environment capability.
- Power Automate cloud flows.
- Standard connectors where available: Office 365 Outlook, Office 365 Users, Microsoft Teams, Planner, SharePoint, and AI Builder.
- Power Platform CLI (`pac`) for solution source control, pack/unpack, import/export, solution checker, and test execution.
- PACX (`pacx`) for Dataverse table, column, relationship, workflow, and solution support where it is more direct than `pac`.

## Local Tools Found

- `pac.exe` 2.6.4 at `C:\Users\60217257\.dotnet\tools\pac.exe`
- `pacx.exe` 1.2026.3.195 at `C:\Users\60217257\.dotnet\tools\pacx.exe`
- `XrmToolBox.cmd`
- Git
- GitHub CLI
- Node/npm
- .NET

No `Microsoft.PowerApps*` or `Microsoft.Xrm*` PowerShell modules were found during setup. Use `pac` and `pacx` first; add PowerShell cmdlets only if a later task requires them.

## Later Platform

- Full Dataverse with environment-level ALM.
- Entra ID app registration and security groups.
- Microsoft Graph for calendar, Teams meetings, transcripts, users, groups, files, and org hierarchy.
- Power BI/Fabric reporting over Dataverse governance data.
- Optional Teams meeting extension, bot notifications, and Copilot integration.
