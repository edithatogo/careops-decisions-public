"""Synthetic browser qualification, not a live Dataverse or model-driven-frame test."""
import json
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from threading import Thread

from playwright.sync_api import sync_playwright

root = Path(__file__).resolve().parents[2]
handler = partial(SimpleHTTPRequestHandler, directory=str(root))
server = ThreadingHTTPServer(("127.0.0.1", 0), handler)
thread = Thread(target=server.serve_forever, daemon=True)
thread.start()
try:
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page(viewport={"width": 375, "height": 900})
        page.set_default_timeout(10000)
        errors = []
        requests = []
        page.on("request", lambda request: requests.append(request.url))
        page.on("pageerror", lambda error: errors.append(str(error)))
        prefix = f"http://127.0.0.1:{server.server_address[1]}"
        try:
            page.goto(prefix + "/powerplatform/src/app/credentialing-register/preview.html")
            page.get_by_role("heading", name="Scope recorded", exact=True).wait_for()
            assert page.get_by_text("Synthetic condition: retain the documented limitation.", exact=True).is_visible()
            for value, text in [("revoked", "Recorded revocation"), ("expired", "Recorded grant expired")]:
                page.select_option("#scenario", value)
                page.get_by_role("heading", name=text, exact=True).wait_for()
                assert page.get_by_text("Source verification is not current or is unavailable.", exact=True).is_visible()
            page.select_option("#scenario", "missing")
            page.get_by_text("Recorded scope is unavailable. No decision or scope has been changed.", exact=True).wait_for()
            assert page.get_by_role("heading", name="Conditions and limitations").count() == 0
            page.select_option("#scenario", "recorded")
            button = page.get_by_role("button", name="Refresh snapshot")
            button.focus()
            page.keyboard.press("Enter")
            page.get_by_role("heading", name="Scope recorded", exact=True).wait_for()
            assert button.evaluate("el => document.activeElement === el")
            assert page.evaluate("document.documentElement.scrollWidth <= window.innerWidth")
            page.screenshot(path=str(root / "synthetic-register-preview.png"), full_page=True)
            page.goto(prefix + "/powerplatform/src/app/credentialing-register/scope-card.html")
            assert page.get_by_role("button", name="Refresh snapshot").is_disabled()
            assert page.get_by_text("No record is connected. Open this panel from an approved register form.", exact=True).is_visible()
            assert not errors, errors
            assert all(url.startswith(prefix + "/") for url in requests), requests
            receipt = {"browser": browser.version, "viewport": "375x900", "scenarios": 4,
                       "keyboard_refresh": True, "horizontal_overflow": False, "page_errors": errors,
                       "native_dataverse_test": False, "external_requests": False, "unbound_page_checked": True}
            (root / "browser-smoke-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
            print(json.dumps(receipt))
        except Exception:
            # Only fixed synthetic fixtures are served by this test, never practitioner records.
            diagnostics = root / "artifacts/scope-card"
            diagnostics.mkdir(parents=True, exist_ok=True)
            (diagnostics / "browser-failure.json").write_text(json.dumps({"page_errors": errors}, indent=2) + "\n")
            print("Synthetic browser errors:", json.dumps(errors))
            page.screenshot(path=str(diagnostics / "browser-failure.png"), full_page=True)
            raise
        finally:
            browser.close()
finally:
    server.shutdown()
    server.server_close()
