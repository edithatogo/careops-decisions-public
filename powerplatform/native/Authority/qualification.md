# N02 native authority source qualification

24 September 2026. Continues Decisions#14 and portfolio CareOps#165; source-only.

The existing in-progress Decisions#15 has been reviewed and continued. Artifact
10755958118 from run 35873170914 at 620bfd72860318faba3740f27b54378c0919f7ee
was downloaded and inspected: 18/18 tests executed/passed, zero skips, 132/132
lines and 108/108 branches. Artifact SHA-256:
709ba4bf7e3888e528c2fd64b469c2a040e96ff90528b5e696ce4d83d5e4e281.
Its generated dependency locks are committed here. Final qualification uses
locked restore and repeats all checks after the changes below.

Review fix: the enum default was Permitted. It is now Unverified (zero), with an
explicit nonzero Permitted value. Consumers must accept only that exact enum
value, never the absence of an error or a default native output. Added tests for
zero/undefined values and an independently calculated 225-byte native-v1 digest
vector. No valid delegation rule or coverage threshold was relaxed.

The pre-existing source branch also fixes the synthetic SQLite concurrency test's
scheduling: real competing connections remain, but the first writer holds a known
transaction before its competitor starts. This removes the incorrect assumption
that a free-running zero-timeout race must always produce exactly one winner.
No SQLite production deployment or extension is made.

Microsoft's current English supported-customizations documentation allows
Dataverse .NET Framework 4.6.2 through 4.8 and recommends 4.8 for new plug-ins.
This existing slice keeps its net462 compilation target and net48 execution tests;
confirm and update the final plug-in package target during native tenant packaging.
https://learn.microsoft.com/en-us/power-apps/developer/data-platform/supported-customizations

SourceVerified, withdrawal, approval, identities, occupancy and time are trusted
HOST inputs, not data accepted from a user/agent. This library is not the host,
authentication, an app registration, a signed receipt or permission to deploy.
Current signed CHHHS instruments, native tables/API access controls and actual
sandbox tests remain open. The no-admin pilot is unchanged. Parent N02 is not
complete; final exact-head CI and source review must pass before merge.
