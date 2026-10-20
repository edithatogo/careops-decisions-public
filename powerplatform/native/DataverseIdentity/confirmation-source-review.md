# Original source acquisition: pre-qualification review

25 September 2026. Companion to native-confirmation-source.md.

Corrected metadata BEFORE qualification: a primary name and columns used by an
alternate key cannot be column-secured; lookup columns also do not support that
control. The primary name and canonical key therefore contain only opaque source
identifiers and IsSecured=false; the record lookup also remains unprotected at
column level. All are still subject to native row/team security. The other source
content columns request column security plus auditing. Explicit tests bind those
exceptions to the actual metadata rather than assuming every column supports it.
This is a supported metadata design, not removal of a working security control.

Native source readers must verify full field/row access. Secured null/missing
content is rejected, never treated as an unrestricted or unwithdrawn confirmation.
Column security cannot verify human action or replace the missing capture/direct-
Create/immutable-source boundary. Neither a successful SDK build nor application-
required metadata certifies native installation or server-side integrity.

Primary Microsoft sources checked 25 September 2026:
- https://learn.microsoft.com/en-us/power-apps/developer/data-platform/define-alternate-keys-entity
- https://learn.microsoft.com/en-us/power-platform/admin/field-level-security

The null native-row check also uses an explicit typed guard so strict nullable
analysis can prove subsequent access, without suppressed warnings. Existing
packages, tests and native qualification thresholds remain unchanged.
