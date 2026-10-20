# Open Source Template Review

Date: 2026-05-02
Scope: whether public Microsoft Teams, Power Apps, and Power Platform kits should be reused, referenced, or avoided for a governance meeting system inspired by public Decisions for Teams sources.

## Recommendation summary

| Source | Reuse stance | Rationale |
| --- | --- | --- |
| Microsoft Teams Power Apps app templates | Reference selectively | Useful examples for Teams-friendly app structure, Dataverse for Teams packaging, theming, and common workflows. None is a meeting governance app. |
| Power Apps samples | Reference selectively | Good technical examples for Dataverse, model-driven apps, PCF, custom connectors, portals, and AI Builder. Too broad to import wholesale. |
| Creator Kit | Reuse | Best candidate for production UI components because it brings Fluent UI-style controls and templates for Power Apps. |
| CoE Starter Kit | Reference, do not embed | Strong governance patterns, inventory/reporting ideas, and admin operating model. It solves Power Platform governance, not meeting governance. |
| Power Apps Test Engine | Reuse for validation | Open-source automated testing path for canvas/model-driven apps, YAML/Power Fx tests, connector mocks, screenshots, and pipeline checks. |
| Decisions public product sources | Reference only | Use for feature mapping and benchmark expectations. Do not copy proprietary workflows, branding, screenshots, text, or implementation details. |

## Source notes

### Microsoft Teams Power Apps app templates

Source: https://github.com/microsoft/teams-powerapps-app-templates
Supporting docs: https://learn.microsoft.com/en-us/power-apps/teams/create-apps-overview and https://learn.microsoft.com/en-us/power-apps/teams/customize-sample-apps

The Teams Power Apps template repo contains sample apps such as Boards, Bulletins, Employee Ideas, Inspection, Issue Reporting, Milestones, Perspectives, Profile+, and Get Connected. These are useful references for:

- Teams app packaging and installation.
- Dataverse for Teams table/app/flow composition.
- Manager/user split patterns.
- Theming, localization, sample-data removal, and publish-to-Teams flow.
- Lightweight workflow apps embedded where users already collaborate.

Reuse decision: reference selectively. Milestones may inform project/action tracking; Perspectives may inform agenda-topic discussion; Bulletins may inform communications; Issue Reporting and Inspection may inform structured intake/review flows. Do not start from any one template as the base application because the target domain needs a purpose-built meeting, decision, vote, minutes, and action data model.

### Power Apps samples

Source: https://github.com/microsoft/PowerApps-Samples

The samples repo covers Power Apps, Dataverse, model-driven apps, canvas apps, PCF, portals, AI Builder, build tools, and custom connectors. It is useful as implementation reference when specific technical needs appear, for example:

- Dataverse API and table operations.
- Model-driven app patterns for the decision register/admin surface.
- PCF controls if standard controls cannot represent agenda/vote interactions well.
- Custom connector examples for integration gaps.

Reuse decision: reference selectively. Avoid copying unrelated sample architecture into the product. Pull only the minimum pattern needed for a specific implementation problem.

### Creator Kit

Source: https://learn.microsoft.com/en-us/power-platform/guidance/toolkits/
Intro: https://www.microsoft.com/en-us/power-platform/blog/power-apps/introducing-the-creator-kit-efficiently-create-performant-fluent-ui-based-power-apps/

The Creator Kit is a Microsoft Power CAT kit with Fluent UI-based controls and templates intended for production Power Apps. It is the strongest reuse candidate for the app shell and controls because a governance meeting system should feel native in Microsoft 365 and Teams.

Reuse decision: reuse where licensing/environment constraints allow. Prefer Creator Kit components for command bars, panels, dialogs, navigation, lists, people/role displays, and status indicators. Keep custom UI minimal unless domain-specific controls are needed.

### CoE Starter Kit

Source: https://learn.microsoft.com/en-us/power-platform/guidance/coe/overview
Repo: https://github.com/microsoft/coe-starter-kit

The CoE Starter Kit is designed for Power Platform adoption, administration, governance, nurture, support, and operations. It includes Dataverse data models, flows, Power Apps, and Power BI reports for tenant-level Power Platform oversight.

Reuse decision: reference, do not embed. It is not a meeting-management foundation. Use it for governance operating-model inspiration, admin reporting conventions, environment/inventory thinking, and lifecycle discipline. Avoid taking a dependency on the kit for a meeting app because it would add unrelated operational scope and upgrade burden.

### Power Apps Test Engine

Sources: https://learn.microsoft.com/en-us/power-platform/release-plan/2025wave1/data-platform/test-power-apps-test-engine and https://github.com/microsoft/PowerApps-TestEngine

Power Apps Test Engine is an open-source automated testing platform for Power Apps. Public materials describe Power Fx-authored YAML tests, canvas and model-driven app testing, connector mocking, screenshots/video, and pipeline integration.

Reuse decision: reuse for validation. Target early tests for:

- Agenda item creation and approval.
- Decision capture from agenda item.
- Vote recording and permission boundaries.
- Action assignment and status transitions.
- Minutes generation preconditions.
- Planner/To Do sync flow guards using mocked connectors where possible.

### Microsoft 365 app certification and Marketplace benchmark

Sources: https://marketplace.microsoft.com/en-us/product/office/WA104381880 and https://learn.microsoft.com/en-us/microsoft-365-app-certification/teams/decisions

The Decisions listing is useful for public feature inventory. The Microsoft 365 certification entry is useful as a disclosure benchmark: supported clients, hosting model, data processed/stored, retention, third-party transfer, and support contact are all surfaced in a governance-facing format.

Reuse decision: reference only. The custom system should have its own comparable data-handling statement before deployment: where data lives, what Graph/Planner/SharePoint identifiers are stored, whether any content leaves the tenant, retention after decommissioning, and who can access sensitive records.

## Build posture

Recommended posture for the meeting system:

1. Build a purpose-specific Dataverse model for meetings, agenda items, decisions, votes, actions, and minutes.
2. Use Creator Kit for Fluent, Teams-aligned Power Apps UI.
3. Use Teams Power Apps templates as pattern references, especially for Teams installation, theming, and split manager/user experiences.
4. Use CoE Starter Kit only as a governance and operational reporting reference.
5. Use Power Apps Test Engine from the start for regression coverage around the meeting lifecycle.
6. Keep Decisions public sources as feature and compliance benchmarks, not as source material to reproduce.

## Avoid list

- Avoid cloning Decisions branding, copy, screenshots, or proprietary flows.
- Avoid making a generic project-management app first; governance decisions and minutes must be first-class.
- Avoid using SharePoint lists as the only decision register if auditability, role security, or structured voting matter.
- Avoid adding AI-generated minutes before permissions, data boundaries, human approval, and retention are documented.
- Avoid importing a large starter kit as a base when the app needs only a few patterns from it.
