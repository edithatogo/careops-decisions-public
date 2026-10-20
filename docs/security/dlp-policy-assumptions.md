# DLP Policy Assumptions And Recommendations

## Principle

DLP policies are owned by Power Platform tenant administrators. These
recommendations describe what a compliant DLP configuration should look like,
but do not assume the ability to create or modify DLP policies. Implementation
teams should present this document to the tenant DLP owner for review.

## Connector Risk Classification

| Connector | Recommended DLP Action | Risk Notes |
| --------- | --------------------- | ---------- |
| Dataverse | Allow (business) | System of record; required for all flows |
| Office 365 Outlook | Allow (business) | Meeting invite sync; calendar data only |
| Microsoft Teams | Allow (business) | Summary posts and notifications |
| Planner | Allow (business) | Action task sync; task data only |
| SharePoint | Allow (business) | Document registration and metadata sync |
| Office 365 Users | Allow (business) | Org hierarchy and directory lookup |
| AI Builder | Allow (business) | Transcript-to-minutes processing; prompt governance required |
| Microsoft Entra ID | Block until approved | Future enterprise path only |
| Microsoft Graph | Block until approved | Requires admin consent and app registration |
| Azure services | Block until approved | Requires subscription and RBAC |
| HTTP with Azure AD | Block until approved | Custom connector path requires security review |
| HTTP (generic) | Block | No unauthenticated outbound calls |
| RSS, News, other non-business | Block | Not relevant to governance workflows |

## DLP Policy Structure Recommendation

One environment-scoped DLP policy applied to the development, test, and
production environments:

```
Policy: Decisions Workspace - Standard
Scope: Environment group (dev, test, prod)
Classification: Business
  - Dataverse
  - Office 365 Outlook
  - Microsoft Teams
  - Planner
  - SharePoint
  - Office 365 Users
  - AI Builder
Classification: Blocked
  - All other connectors not listed above
```

## Key Assumptions

1. **No tenant-level DLP exists that blocks business connectors in the
   target environments.** If a tenant-level policy already blocks any of the
   required connectors, the environment-scoped policy recommendation is
   insufficient and the tenant DLP owner must create an exception.

2. **AI Builder is treated as a business connector**, not an AI preview
   connector. If the tenant classifies AI Builder separately, the policy
   must be adjusted to allow AI Builder in the business data group.

3. **HTTP and custom connector use is zero for MVP.** No flow makes
   unauthenticated HTTP calls or uses custom connectors. If a future
   integration requires a custom connector, a DLP policy update must be
   requested before development.

4. **DLP does not replace record-level permissions.** DLP controls which
   connectors flows can use, but does not control which records a user can
   read or write. Security roles, access teams, and access rules provide
   record-level control.

## Recommendation For Teams Without Tenant Admin Access

If you cannot create or view DLP policies:

1. Confirm with your Power Platform admin that the business data group
   policy described above exists or can be created.
2. Request a read-only DLP view (`Power Platform Admin` role or environment
   admin) to verify the policy after creation.
3. Block-listed connectors: if a required connector (e.g., Teams or Planner)
   appears in the blocked group, your flows will fail at runtime. Do not
   deploy until the DLP is corrected.
4. Document any DLP exception requests and track them as deployment blockers
   in `docs/readiness/implementation-readiness-gates.md`.
