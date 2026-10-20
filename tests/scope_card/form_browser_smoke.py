"""Real-browser integration of the delivered scripts with a synthetic native-form facade.

No Dataverse connection, actual permission assertion or real practitioner data.
The platform's frame/event/API contract still needs separate tenant qualification.
"""
import argparse
import json
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from threading import Thread

from playwright.sync_api import sync_playwright

SETUP = r"""() => {
    const A = '10000000-0000-0000-0000-000000000001';
    const B = '20000000-0000-0000-0000-000000000002';
    const state = { A, B, id: A, sectionVisible: true, expanded: 'expanded',
        calls: [], holdNext: false, pending: null, dataHooks: new Set(), tabHooks: new Set() };
    const tab = { getName: () => 'credentialing', getVisible: () => true,
        getDisplayState: () => state.expanded,
        addTabStateChange: f => state.tabHooks.add(f), removeTabStateChange: f => state.tabHooks.delete(f) };
    const section = { getVisible: () => state.sectionVisible, getParent: () => tab };
    const control = { getControlType: () => 'webresource', getVisible: () => true, getParent: () => section,
        getContentWindow: async () => document.getElementById('register-frame').contentWindow };
    const form = { getControl: name => name === 'WebResource_scope' ? control : null,
        ui: { tabs: { get: name => name === 'credentialing' ? tab : null } },
        data: { entity: { getEntityName: () => 'dft_credentialingrecord', getId: () => '{' + state.id + '}' },
            addOnLoad: f => state.dataHooks.add(f), removeOnLoad: f => state.dataHooks.delete(f) } };
    function response(id) {
        const label = id === A ? 'A' : 'B';
        return { ok: true, json: async () => ({ ReadSucceeded: true,
            ReadDisposition: 'recorded-data-only-not-practice-clearance', CurrentnessVerified: false,
            HasEffectiveDecision: true, RecordedStatus: 'ScopeRecorded', LatestSequence: 1, SelectedSequence: 1,
            RootVersion: '9', AsOfUtc: '2026-09-24T00:00:00Z', SelectedDecision: {
                dft_credentialingdecisionid: '30000000-0000-0000-0000-000000000003', _dft_record_value: id,
                dft_sequence: 1, dft_recordedstate: 602170001,
                dft_validfrom: '2026-09-24T00:00:00Z', dft_validuntil: '2026-10-24T00:00:00Z',
                dft_recordedat: '2026-09-24T00:00:00Z', dft_effectiveat: '2026-09-24T00:00:00Z',
                dft_scopeversionref: 'SYNTHETIC-' + label, dft_scopetext: 'Synthetic scope ' + label,
                dft_conditionstext: 'Synthetic condition ' + label, dft_supervisiontext: 'Synthetic supervision ' + label
            } }) };
    }
    const online = { execute: async function(request) {
        if (this !== online || request.getMetadata().operationName !== 'dft_ReadCurrentRecordedScopeV1') throw new Error('Unexpected facade call');
        const id = request.entity.id;
        if (id !== A && id !== B) throw new Error('Unknown synthetic fixture');
        state.calls.push(id);
        if (state.holdNext) {
            state.holdNext = false;
            return new Promise(resolve => { state.pending = () => { state.pending = null; resolve(response(id)); }; });
        }
        return response(id);
    } };
    globalThis.Xrm = { WebApi: { online } }; // Synthetic facade only, never a real authentication claim.
    state.bind = () => CareOpsScopeForm.onLoad({ getFormContext: () => form }, 'WebResource_scope', 'credentialing');
    state.emitData = () => Promise.all([...state.dataHooks].map(f => f()));
    state.emitTab = () => Promise.all([...state.tabHooks].map(f => f()));
    globalThis.formFixture = state;
    state.controller = state.bind();
}"""


def run(executable: str | None = None) -> None:
    root = Path(__file__).resolve().parents[2]
    output = root / "artifacts/scope-card"
    output.mkdir(parents=True, exist_ok=True)
    handler = partial(SimpleHTTPRequestHandler, directory=str(root))
    server = ThreadingHTTPServer(("127.0.0.1", 0), handler)
    Thread(target=server.serve_forever, daemon=True).start()
    try:
        with sync_playwright() as p:
            browser = p.chromium.launch(headless=True, executable_path=executable)
            page = browser.new_page(viewport={"width": 375, "height": 950})
            page.set_default_timeout(10000)
            errors: list[str] = []
            requests: list[str] = []
            page.on("pageerror", lambda error: errors.append(str(error)))
            page.on("request", lambda request: requests.append(request.url))
            prefix = f"http://127.0.0.1:{server.server_address[1]}"
            try:
                page.goto(prefix + "/tests/scope_card/form-host.html")
                frame = page.frame(name="register-card")
                assert frame is not None
                assert frame.evaluate("typeof CareOpsScopeCard === 'object'")
                assert frame.get_by_role("button", name="Refresh snapshot").is_disabled()
                page.evaluate(SETUP)
                frame.get_by_text("Synthetic scope A", exact=True).wait_for()
                assert frame.get_by_text("Synthetic condition A", exact=True).is_visible()
                assert frame.get_by_text("Synthetic supervision A", exact=True).is_visible()
                button = frame.get_by_role("button", name="Refresh snapshot")
                button.focus()
                page.keyboard.press("Enter")
                frame.get_by_text("Synthetic scope A", exact=True).wait_for()
                assert button.evaluate("el => document.activeElement === el")

                # The existing card must ignore a delayed response after the form moves to record B.
                page.evaluate("() => { formFixture.holdNext = true; return formFixture.controller.refresh(); }")
                assert page.evaluate("formFixture.pending !== null")
                frame.get_by_text("Reading the register…", exact=True).wait_for()
                page.evaluate("() => { formFixture.id = formFixture.B; return formFixture.emitData(); }")
                frame.get_by_text("Synthetic scope B", exact=True).wait_for()
                page.evaluate("async () => { formFixture.pending(); await new Promise(resolve => setTimeout(resolve, 0)); }")
                assert frame.get_by_text("Synthetic scope A", exact=True).count() == 0
                assert frame.get_by_text("Synthetic condition B", exact=True).is_visible()

                before = page.evaluate("formFixture.calls.length")
                page.evaluate("() => { formFixture.expanded = 'collapsed'; return formFixture.emitTab(); }")
                assert frame.locator("#scope-card").inner_text() == ""
                assert frame.get_by_role("button", name="Refresh snapshot").is_disabled()
                assert page.evaluate("formFixture.calls.length") == before
                page.evaluate("() => { formFixture.expanded = 'expanded'; return formFixture.emitTab(); }")
                frame.get_by_text("Synthetic scope B", exact=True).wait_for()

                before = page.evaluate("formFixture.calls.length")
                page.evaluate("() => { formFixture.sectionVisible = false; return formFixture.emitData(); }")
                assert frame.locator("#scope-card").inner_text() == ""
                assert page.evaluate("formFixture.calls.length") == before
                page.evaluate("() => { formFixture.sectionVisible = true; return formFixture.emitData(); }")
                frame.get_by_text("Synthetic scope B", exact=True).wait_for()

                # Repeated native initialization must not accumulate handlers.
                page.evaluate("() => { formFixture.controller = formFixture.bind(); formFixture.controller = formFixture.bind(); }")
                frame.get_by_text("Synthetic scope B", exact=True).wait_for()
                assert page.evaluate("[formFixture.dataHooks.size, formFixture.tabHooks.size]") == [1, 1]

                # Exercise actual browser frame destruction/recreation, not merely a fake window object.
                page.evaluate("document.getElementById('register-frame').src += '?recreated=1'")
                frame.wait_for_url("**/scope-card.html?recreated=1", wait_until="load")
                frame = page.frame(name="register-card")
                assert frame is not None
                assert frame.evaluate("typeof CareOpsScopeCard === 'object'")
                assert frame.get_by_role("button", name="Refresh snapshot").is_disabled()
                page.evaluate("() => formFixture.emitTab()")
                frame.get_by_text("Synthetic scope B", exact=True).wait_for()
                assert frame.evaluate("document.documentElement.scrollWidth <= window.innerWidth")
                page.screenshot(path=str(output / "form-lifecycle.png"), full_page=True)

                page.evaluate("formFixture.controller.dispose()")
                assert frame.locator("#scope-card").inner_text() == ""
                assert frame.get_by_role("button", name="Refresh snapshot").is_disabled()
                assert page.evaluate("[formFixture.dataHooks.size, formFixture.tabHooks.size]") == [0, 0]
                assert not errors, errors
                assert all(url.startswith(prefix + "/") for url in requests), requests
                receipt = {
                    "browser": browser.version, "synthetic_native_form_facade": True, "native_dataverse_test": False,
                    "script_composition": ["form-binding.js", "scope-card.js"], "automatic_initial_read": True,
                    "keyboard_refresh_focus": True, "late_response_after_record_change_rejected": True,
                    "collapsed_tab_no_read": True, "hidden_section_no_read": True,
                    "idempotent_initialization": True, "actual_iframe_recreation": True,
                    "dispose_clears_output_and_hooks": True, "external_requests": False, "page_errors": errors,
                    "viewport": "375x950", "credentialing_decision_performed": False,
                }
                (output / "form-browser-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
                print(json.dumps(receipt))
            except Exception:
                (output / "form-browser-failure.json").write_text(json.dumps({"page_errors": errors, "requests": requests}, indent=2) + "\n", encoding="utf-8")
                page.screenshot(path=str(output / "form-browser-failure.png"), full_page=True)
                raise
            finally:
                browser.close()
    finally:
        server.shutdown()
        server.server_close()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--browser-executable", help="Optional local test browser; hosted CI uses its pinned browser.")
    arguments = parser.parse_args()
    run(arguments.browser_executable)
