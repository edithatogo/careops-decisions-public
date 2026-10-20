# DecisionsWorkspace Solution Source

This folder contains the unpacked `decisionsworkspace` / `CareOps Decisions` solution source exported from Dylan.

Current source layout uses the Power Platform unpacked root form:

- `Other/` for solution manifest, relationships, and customizations.
- `Entities/` for Dataverse table metadata, forms, views, and ribbons.
- `OptionSets/` for global choices.
- `AppModules/` and `AppModuleSiteMaps/` for the model-driven app shell.
- `src/WebResources/` for generated or imported web-resource payloads.

Packing scripts should target this folder as the solution source root. Older nested `src/Other` manifests are treated as legacy compatibility paths only.
