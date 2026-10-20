# Read-only credentialing register card

24 September 2026. N03.07 presentation source and future N07 workbench component.
Sequential source work after Decisions#28; full N03/N07 acceptance remains open.

This card displays the existing current-scope read response in a model-driven
Power Apps form. It does not create another application, register, approval queue
or decision. Only the existing scope-read API is called through the supplied
Microsoft client API. No new Entra app, credentials, HTTP client, package runtime,
external fonts, telemetry or browser persistence is introduced.

## Source assets

- `scope-card.html`, `scope-card.css` and `scope-card.js`: deployable source for a
  same-solution HTML web resource. An unbound page shows no record and has its
  refresh button disabled. These files are NOT a tenant-exported managed solution.
- `preview.html` and `preview.js`: four fixed SYNTHETIC scenarios. These are not
  live reads, document verification or credentialing policy examples. The preview
  content-security policy prohibits network connections; there is no upload or
  practitioner-data entry. Do not install the preview as a production register.

The native form should obtain the resource through the documented control
`getContentWindow()` API, then pass the current canonical record GUID and an
executor calling `Xrm.WebApi.online.execute(request)` to:

```javascript
contentWindow.CareOpsScopeCard.bindPage(
    contentWindow.document,
    canonicalRecordGuid,
    request => Xrm.WebApi.online.execute(request)
);
```

Bind on the current form lifecycle after checking its target and resource identity.
Prevent stale `getContentWindow` callbacks on record/tab navigation, dispose old
controllers when leaving a record, and rebind a re-created control. Do not inspect
unsupported application DOM, supply a role/actor/time claim, build an external
URL, or use an elevated service to overcome a rejected read. The registration,
form event wiring and published Web API response still require native testing.

## Information and failure behaviour

The panel always says recorded data, not permission to practise. It shows recorded
status and source currentness separately, followed by the exact as-of UTC time,
scope, full conditions and supervision, and the recorded validity interval.
It uses textual headings rather than colour-only/green-clearance indicators.
Narratives use text nodes, never HTML, and retain line breaks. Native keyboard
refresh sits outside the polite live region so focus is not destroyed by updates.
A read snapshot may change after retrieval; it is not a continuous safety alert.

The response parser requires literal success/Boolean values, bounded sequences,
canonical versions/identifiers, the same record, explicit conditions and an
internally consistent known recorded status. It copies only approved display
fields; extra/actor/authority/related data cannot pass through. Absent/null
conditions are NOT an empty unrestricted scope. Date comparisons retain all seven
fractional UTC digits (100ns) rather than round an expiry boundary to milliseconds.
No local clock, policy duration or competence judgement is substituted.

A refresh clears the old result immediately. Timeouts, access/HTTP/parse errors
show the same generic unavailable message without private exception text. Late
responses and responses from a disposed/rebound record cannot replace the current
view. The 15-second display timeout does not claim to cancel the transport or
retry it. A failed rebind removes the previous record before validation. There
is no local cache, background poll, automatic applicant return or write.

The parser's expected JSON wire representation is deliberately explicit: nested
SelectedDecision uses `dft_credentialingdecisionid`, `_dft_record_value` and native
scalar fields; dates are explicit UTC strings. This shape has NOT been established
from a CHHHS tenant response. A different/null-filtered representation fails closed
until an explicit versioned mapping and tests are approved. Do not remove record,
restriction or precision checks just to accommodate a sample payload.

## Qualification and remaining gates

Node tests use no npm packages and exercise all delivered JavaScript, including
fixed preview wiring. Local Node22.16.0:23 tests, complete line/branch/function
coverage. CI repeats on the pinned official Node24.21.0 release and runs synthetic
browser/keyboard/narrow-screen checks using pinned test-only Playwright tools.
Local Chromium could not open the loopback test server due to environment policy;
no local browser success is claimed. Inspect actual final-head hosted evidence.
Browser smoke is not assistive-technology, native-frame or clinical acceptance.

Required before a live register view: approved form/web-resource packaging; real
Custom API registration, privileges and caller row/column/export permissions;
exact SDK/Web API response and nullable/empty/time fidelity; record/tab navigation;
screen-reader/zoom/contrast and representative-user testing; appropriate refresh
and outage arrangements. Backend completeness and identity checks remain required:
client validation is not security. No native table or endpoint is installed here.

The existing no-admin Lists/connector-free Flow pilot remains independent. The
static synthetic preview is an additional visual rehearsal, not permission to use
real data without the applicable source/privacy/security approvals. Operators do
not need the development Node/Python test tools when using an approved hosted card.

Primary documentation checked 24 September 2026:
- https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/controls/getcontentwindow
- https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/xrm-webapi/online/execute
- https://github.com/nodejs/node/releases/tag/v24.21.0
- https://github.com/microsoft/playwright-python/releases/tag/v1.63.0
