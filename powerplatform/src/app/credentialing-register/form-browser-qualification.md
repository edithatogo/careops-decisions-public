# Browser composition qualification for the recorded-scope card

24 September 2026. N03.07 source qualification, stacked on Decisions#30 at
18a1348d6accde1e1dd6c7716ceed22ea4d035ed. Full native acceptance remains open.

The existing unit tests exercise the form and card separately. The additional
`tests/scope_card/form_browser_smoke.py` composes the ACTUAL delivered form-binding
and card scripts across a real same-origin browser iframe. Only the native form,
tab and Xrm API objects are test facades. All responses are two fixed synthetic
records; there are no credentials, real practitioner data or Dataverse calls.

Assertions exercise automatic initial read, keyboard refresh and retained focus,
record replacement while an old response is pending, hidden section/collapsed tab,
repeated initialization, actual iframe document navigation, and teardown of both
native hooks and rendered output. The browser receipt records the real browser
version, executed checks and an explicit native_dataverse_test=false boundary.
Test failure keeps a synthetic screenshot/error receipt and fails existing CI.
The prior standalone four-scenario browser smoke and all44 Node tests remain.

No operator needs to run these tests per case. They provide a reusable developer
regression suite; they do not add a case form, credentialing task or approval.
The synthetic test host is NOT a production form or additional application.

A local browser attempt was stopped by its administrator navigation policy before
the host loaded. That policy was not bypassed and no local browser pass is claimed.
Use actual completed hosted CI evidence, not file presence or a fabricated receipt.
The existing approved GitHub workflow uses its pinned test-only Playwright browser.
No additional package or test threshold change is required by this source slice.

Initial hosted execution reached the iframe-recreation check but the TEST's
string-eval polling conflicted with its strict CSP. Replaced that polling with
Playwright's native frame.wait_for_url(load) navigation event and explicit readiness
assertions; did not add unsafe-eval, disable CSP or remove the iframe assertions.
https://playwright.dev/python/docs/api/class-frame#frame-wait-for-url
The final-head browser suite must pass after this correction before merge.

Even a successful composed browser test does not establish the live native form
lifecycle, platform CSP/origin, actual Web API response representation, record
permissions, screen-reader behavior or clinical acceptance. Those remain explicit
N03/N07 native sandbox gates. The no-admin Lists/Flow and fixed visual pilot remain
unchanged; no tenant provisioning, app registration, writes, messages or CGov change.
