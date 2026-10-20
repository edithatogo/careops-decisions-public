# Native form lifecycle binding for the recorded-scope card

24 September 2026. N03.07/N03.10 source after Decisions#29 at
88e44563b0645a50c88a6e670341c2ea8c4b4a17. Not installed in a tenant.

`form-binding.js` is a model-driven FORM library, not a script that reaches into
`parent.Xrm` from an arbitrary web page. `CareOpsScopeForm.onLoad` obtains the
current form through the native execution context, resolves the configured HTML
web-resource window with getContentWindow and binds the existing card to the
current saved dft_credentialingrecord. Existing Xrm.WebApi.online.execute supplies
the authenticated platform session; there is no token, supplied app ID, custom
HTTP connector, user-name lookup or permission fallback.

The form and configured tab each get one removable handler. Data reload and tab
expansion rebind; collapse, frame pagehide and disposal clear the old snapshot
and disable its refresh button. Repeated initialization replaces rather than
accumulates handlers. Unsaved/wrong-table/hidden or invalid records are not read.
The control's containing section must also be visible and belong to the exact
configured tab; control-level visibility alone does not establish this.
Late frame, native response and JSON-body promises cannot bind to a later record.
A child-supplied operation is not forwarded: the adapter constructs only the
fixed scope-read action and rechecks the current record before and after reads.
This is client lifecycle protection, not authorization or a human decision.

## Finite native setup, subject to approved deployment

After the separately approved API and resources are registered, add this script
as a form library on the Decisions-owned register form. Register
`CareOpsScopeForm.onLoad`, pass execution context, and supply two exact strings:
the existing HTML resource CONTROL name and its TAB name. Example configuration:
`"WebResource_scope", "credentialing"`; these are examples, not created controls.
Do not also register a duplicate manual TabStateChange handler. The code manages
its tab/data handlers. A custom host must call the returned controller's dispose
when retiring the form; native frame/tab/navigation behavior must be qualified.
The card loads automatically during the existing review, with no new form, queue,
copy/paste task or repeated credentialing approval. There is no timed polling.

## Evidence and remaining gates

Twenty local Node22.16.0 tests pass, including malformed identities, tab/section
state, response races, hooks, failed cleanup, cross-record reads and fixed-request
construction. Real named-source V8 line/branch/function coverage is 100%; tests
set an explicit VM filename and CI requires its coverage row, so empty coverage
cannot masquerade as a pass. The existing 24 card/preview tests are retained.
Hosted combined tests and native-framework tests must pass before merge.

Review identified that a visible control can belong to a hidden section. Added
parent-section visibility and tab ownership checks, with dedicated regressions;
no existing check or coverage threshold was weakened.

Mocks are not actual Dataverse forms. Native API wire format, permissions, form
record/navigation lifecycle, frame origin/CSP, browser and assistive-technology
acceptance remain open. The existing synthetic browser preview and no-admin
Lists/Flow rehearsal are unchanged. No native record, app registration, licence,
permission, authority, message or CGov setting is changed by these sources.
An as-of read is not continuous monitoring or permission to practise.

Microsoft primary sources checked 24 September 2026:
- https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/controls/getcontentwindow
- https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/formcontext-data/addonload
- https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/formcontext-ui-tabs
- https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/xrm-webapi/online/execute
