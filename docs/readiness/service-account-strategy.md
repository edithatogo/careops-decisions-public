# Service Account And Connection Ownership Strategy

## MVP Default

Use named development connections only for local development and testing. Record the connection owner, connector, purpose, and environment in deployment notes.

## Shared Environment Target

Use dedicated service accounts or managed connection ownership where tenant policy allows. Avoid personal accounts for shared or production flows.

## Required Connection Ownership Records

- Outlook invite sync connection owner.
- Teams notification connection owner.
- Planner task sync connection owner.
- SharePoint document registration connection owner.
- Office 365 Users hierarchy sync connection owner.
- AI Builder prompt runner owner, if enabled.

## Future Azure/Entra

When access exists, prefer app registrations, managed identities, and service principals over user-owned credentials for background services.
