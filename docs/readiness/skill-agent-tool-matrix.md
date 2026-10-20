# Skill, Agent, And Tool Matrix

| Track | Primary Agent | Skills | CLI And Tools | Notes |
| --- | --- | --- | --- | --- |
| 22 App-core | Six gpt-5.4-mini medium workers | conductor-status, conductor-implement, conductor-test, dataverse | `pac`, `pacx`, Git, GitHub CLI, Node validators | Worker scopes: activation baseline; release artifacts and solution checker; sample data and configuration wizard; human review gates; reporting; tests and validation |
| 14 ALM | Worker A | conductor-implement, conductor-test, dataverse | `pac`, `pacx`, Git, GitHub CLI | Owns solution packaging and release gates |
| 15 Security | Worker B | conductor-implement, conductor-review, dataverse, azure-compliance | XrmToolBox, `pac`, `pacx` | Owns roles, DLP, privacy, retention |
| 16 Reporting | Worker F | Power BI Modeling, Power BI Security, Power BI Report | Dataverse views, Power BI later | Owns Dataverse dashboards and future semantic model |
| 17 Operations | Worker G | conductor-implement, conductor-test, azure-validate | Power Automate run history, future Azure monitoring | Owns support runbooks and alerting |
| 18 Data Bootstrap | Worker H | dataverse, data-validation | `pacx`, Node/npm validators | Owns seed, import, migration policy |
| 19 Testing | Worker I | conductor-test, Power Apps Code Apps | Power Apps Test Engine, Node/npm, GitHub Actions later | Owns CI and acceptance evidence |
| 20 Readiness | Worker J | conductor-status, azure-prepare, azure-rbac, entra-app-registration | `pac`, `pacx`, tenant inventory later | Owns ADRs, licensing, permissions, gates |

Explorer agents should verify tool capabilities, source-map gaps, template options, and validation gaps without mutating implementation files.
