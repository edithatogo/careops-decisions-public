"use strict";
const assert = require("node:assert/strict");
const { test } = require("node:test");
const fs = require("node:fs"), path = require("node:path"), vm = require("node:vm");
const filename = path.resolve(__dirname, "../../powerplatform/src/app/credentialing-register/form-binding.js");
const source = fs.readFileSync(filename, "utf8");
const A = "10000000-0000-0000-0000-000000000001", B = "20000000-0000-0000-0000-000000000002";
const tick = () => new Promise(resolve => setImmediate(resolve));
function deferred() { let resolve, reject; const promise = new Promise((yes, no) => { resolve = yes; reject = no; }); return { promise, resolve, reject }; }
function load() { const sandbox = {}; vm.runInNewContext(source, sandbox, { filename }); return { api: sandbox.CareOpsScopeForm, sandbox }; }
function frame() {
    const container = { values: [], replaceChildren() { this.values = []; } }, button = { disabled: true, onclick: null };
    const listeners = new Set(), calls = [], controllers = [];
    const document = { getElementById: id => id === "scope-card" ? container : button };
    const window = {
        document, addEventListener(name, fn) { assert.equal(name, "pagehide"); listeners.add(fn); },
        removeEventListener(name, fn) { assert.equal(name, "pagehide"); listeners.delete(fn); },
        CareOpsScopeCard: { bindPage(doc, id, execute) {
            assert.equal(doc, document); calls.push({ id, execute }); container.values = [id]; button.disabled = false;
            const controller = { disposed: 0, refresh() {}, dispose() { this.disposed++; container.replaceChildren(); } };
            controllers.push(controller); return controller;
        } }
    };
    return { window, container, button, listeners, calls, controllers };
}
function fixture() {
    const view = frame(), dataHooks = new Set(), tabHooks = new Set();
    const state = { id: "{" + A.toUpperCase() + "}", table: "dft_credentialingrecord", visible: true, sectionVisible: true, tabVisible: true, expanded: "expanded" };
    const control = { getParent: () => section, getControlType: () => "webresource", getVisible: () => state.visible, getContentWindow: async () => view.window };
    const tab = { getName: () => "credentialing", getVisible: () => state.tabVisible, getDisplayState: () => state.expanded,
        addTabStateChange: fn => tabHooks.add(fn), removeTabStateChange: fn => tabHooks.delete(fn) };
    const section = { getVisible: () => state.sectionVisible, getParent: () => tab };
    const form = { getControl: name => { assert.equal(name, "WebResource_scope"); return control; }, ui: { tabs: { get: name => { assert.equal(name, "credentialing"); return tab; } } },
        data: { entity: { getEntityName: () => state.table, getId: () => state.id }, addOnLoad: fn => dataHooks.add(fn), removeOnLoad: fn => dataHooks.delete(fn) } };
    const sent = [], response = { ok: true, json: async () => ({ synthetic: true }) };
    const execute = async request => { sent.push(request); return response; };
    const { api, sandbox } = load();
    const attach = () => api.attach(form, "WebResource_scope", "credentialing", execute);
    return { api, sandbox, state, view, control, tab, form, dataHooks, tabHooks, sent, response, execute, attach };
}
function request(id = A) { return { entity: { entityType: "dft_credentialingrecord", id } }; }

test("binds one recorded-scope card using only a fixed native read request", async () => {
    const f = fixture(), binding = f.attach(); await tick();
    assert.equal(f.view.calls.length, 1); assert.equal(f.view.calls[0].id, A);
    const malicious = request(); malicious.getMetadata = () => { throw new Error("must not execute supplied action"); };
    const result = await f.view.calls[0].execute(malicious);
    assert.deepEqual(await result.json(), { synthetic: true }); assert.equal(f.sent.length, 1);
    const sent = f.sent[0]; assert.equal(sent.entity.id, A); assert.equal(Object.isFrozen(sent), true);
    const metadata = sent.getMetadata(); assert.equal(metadata.operationName, "dft_ReadCurrentRecordedScopeV1");
    assert.equal(metadata.operationType, 0); assert.equal(metadata.boundParameter, "entity");
    assert.equal(metadata.parameterTypes.entity.typeName, "mscrm.dft_credentialingrecord");
    assert.equal(metadata.parameterTypes.entity.structuralProperty, 5);
    binding.dispose(); assert.equal(f.view.button.disabled, true); assert.equal(f.view.button.onclick, null);
    assert.equal(f.view.container.values.length, 0); assert.equal(f.dataHooks.size, 0); assert.equal(f.tabHooks.size, 0);
});

test("onLoad uses the native receiver and retains no supplied token or app ID", async () => {
    const f = fixture(); const online = { execute(value) { assert.equal(this, online); return f.execute(value); } };
    f.sandbox.Xrm = { WebApi: { online } };
    const c = f.api.onLoad({ getFormContext: () => f.form }, "WebResource_scope", "credentialing");
    assert.ok(c); await tick(); await f.view.calls[0].execute(request()); assert.equal(f.sent.length, 1); c.dispose();
    assert.equal(f.api.onLoad(null, "x", "y"), null);
    delete f.sandbox.Xrm; assert.equal(f.api.onLoad({ getFormContext: () => f.form }, "WebResource_scope", "credentialing"), null);
    f.sandbox.Xrm = { WebApi: { online: { execute: 4 } } };
    assert.equal(f.api.onLoad({ getFormContext: () => f.form }, "WebResource_scope", "credentialing"), null);
});

test("record reload clears the old frame and rebinds the new ID once", async () => {
    const f = fixture(), c = f.attach(); await tick(); f.state.id = B;
    await [...f.dataHooks][0](); assert.equal(f.view.controllers[0].disposed, 1);
    assert.equal(f.view.calls[1].id, B); assert.deepEqual(f.view.container.values, [B]);
    await assert.rejects(f.view.calls[0].execute(request())); assert.equal(f.sent.length, 0); c.dispose();
});

test("collapsed, hidden and wrong-table forms make no card or data call", async () => {
    for (const [field, value] of [["visible", false], ["sectionVisible", false], ["tabVisible", false], ["expanded", "collapsed"], ["table", "other"]]) {
        const f = fixture(); f.state[field] = value; const c = f.attach(); await tick();
        assert.equal(f.view.calls.length, 0); assert.equal(f.sent.length, 0); c.dispose();
    }
});

test("unsaved and malformed IDs never fall back to names or another record", async () => {
    for (const id of [null, 1, "", "x", "00000000-0000-0000-0000-000000000000", "{" + A, A + "}", " " + A, "{{" + A + "}}", "10000000000000000000000000000001"]) {
        const f = fixture(); f.state.id = id; const c = f.attach(); await tick();
        assert.equal(f.view.calls.length, 0); c.dispose();
    }
});

test("tab expansion recreates the binding and collapse destroys previous output", async () => {
    const f = fixture(); f.state.expanded = "collapsed"; const c = f.attach(); await tick();
    f.state.expanded = "expanded"; await [...f.tabHooks][0](); assert.equal(f.view.calls.length, 1);
    f.state.expanded = "collapsed"; await [...f.tabHooks][0]();
    assert.equal(f.view.container.values.length, 0); assert.equal(f.view.button.disabled, true); c.dispose();
});

test("superseded and disposed content-window promises cannot mount", async () => {
    const f = fixture(), first = deferred(), second = deferred(); let n = 0;
    f.control.getContentWindow = () => (++n === 1 ? first.promise : second.promise);
    const c = f.attach(); f.state.id = B; const later = c.refresh();
    second.resolve(f.view.window); await later; first.resolve(f.view.window); await tick();
    assert.equal(f.view.calls.length, 1); assert.equal(f.view.calls[0].id, B); c.dispose();
    const g = fixture(), pending = deferred(); g.control.getContentWindow = () => pending.promise;
    const d = g.attach(); d.dispose(); pending.resolve(g.view.window); await tick();
    assert.equal(g.view.calls.length, 0); await d.refresh(); assert.equal(g.view.calls.length, 0); d.dispose();
});

test("silent record change during frame resolution cannot bind the wrong record", async () => {
    const f = fixture(), pending = deferred(); f.control.getContentWindow = () => pending.promise;
    const c = f.attach(); f.state.id = B; pending.resolve(f.view.window); await tick();
    assert.equal(f.view.calls.length, 0); c.dispose();
});

test("frame rejection does not expose raw platform errors or resurrect content", async () => {
    const f = fixture(); f.control.getContentWindow = async () => { throw new Error("private native error"); };
    const c = f.attach(); await tick(); assert.equal(f.view.calls.length, 0); c.dispose();
    const g = fixture(), pending = deferred(); g.control.getContentWindow = () => pending.promise;
    const d = g.attach(); d.dispose(); pending.reject(new Error("old failure")); await tick(); assert.equal(g.sent.length, 0);
});

test("bad frame exports and malformed controllers clear the read panel", async () => {
    for (const mode of ["null", "document", "events", "namespace", "binding", "null-controller", "dispose", "refresh", "throw"]) {
        const f = fixture();
        if (mode === "null") f.control.getContentWindow = async () => null;
        if (mode === "document") f.view.window.document = null;
        if (mode === "events") f.view.window.addEventListener = null;
        if (mode === "namespace") f.view.window.CareOpsScopeCard = null;
        if (mode === "binding") f.view.window.CareOpsScopeCard.bindPage = null;
        if (mode === "null-controller") f.view.window.CareOpsScopeCard.bindPage = () => null;
        if (mode === "dispose") f.view.window.CareOpsScopeCard.bindPage = () => ({ refresh() {} });
        if (mode === "refresh") f.view.window.CareOpsScopeCard.bindPage = () => ({ dispose() {} });
        if (mode === "throw") f.view.window.CareOpsScopeCard.bindPage = () => { throw new Error("bad frame"); };
        const c = f.attach(); await tick(); assert.equal(f.sent.length, 0); c.dispose();
    }
});

test("pagehide clears sensitive output but permits the next native tab initialization", async () => {
    const f = fixture(), c = f.attach(); await tick(); const old = f.view.calls[0];
    [...f.view.listeners][0](); assert.equal(f.view.listeners.size, 0); assert.equal(f.view.container.values.length, 0);
    await assert.rejects(old.execute(request())); await c.refresh(); assert.equal(f.view.calls.length, 2); c.dispose();
});

test("repeated attach is idempotent and invalid reconfiguration removes the old session", async () => {
    const f = fixture(), first = f.attach(); await tick(); const second = f.attach(); await tick();
    assert.equal(f.dataHooks.size, 1); assert.equal(f.tabHooks.size, 1); assert.equal(f.view.controllers[0].disposed, 1);
    first.dispose(); await second.refresh(); assert.equal(f.view.calls.length, 3);
    assert.throws(() => f.api.attach(f.form, "", "credentialing", f.execute));
    assert.equal(f.dataHooks.size, 0); assert.equal(f.view.container.values.length, 0); second.dispose();
});

test("invalid setup cannot register a partially usable surface", () => {
    const { api } = load(); assert.throws(() => api.attach(null, "x", "y", () => {}));
    assert.throws(() => api.attach(1, "x", "y", () => {}));
    for (const [control, tab, execute] of [[null,"x",()=>{}],["x".repeat(129),"x",()=>{}],["x",null,()=>{}],["x","",()=>{}],["x","x".repeat(129),()=>{}],["x","x",null]]) {
        assert.throws(() => api.attach({}, control, tab, execute));
    }
    for (const mode of ["missing-control", "type", "content", "missing-tab", "tab-add", "tab-remove", "data-add", "data-remove"]) {
        const f = fixture();
        if (mode === "missing-control") f.form.getControl = () => null;
        if (mode === "type") f.control.getControlType = () => "iframe";
        if (mode === "content") f.control.getContentWindow = null;
        if (mode === "missing-tab") f.form.ui.tabs.get = () => null;
        if (mode === "tab-add") f.tab.addTabStateChange = null;
        if (mode === "tab-remove") f.tab.removeTabStateChange = null;
        if (mode === "data-add") f.form.data.addOnLoad = null;
        if (mode === "data-remove") f.form.data.removeOnLoad = null;
        assert.throws(f.attach); assert.equal(f.dataHooks.size, 0); assert.equal(f.tabHooks.size, 0);
    }
});

test("failed registration unwinds existing hooks without swallowing a usable session", () => {
    for (const on of ["data", "tab"]) {
        const f = fixture();
        if (on === "data") f.form.data.addOnLoad = fn => { f.dataHooks.add(fn); throw new Error("fail"); };
        else f.tab.addTabStateChange = fn => { f.tabHooks.add(fn); throw new Error("fail"); };
        assert.throws(f.attach); assert.equal(f.dataHooks.size, 0); assert.equal(f.tabHooks.size, 0);
    }
});

test("cleanup continues if destroyed frames or old native handlers throw", async () => {
    const f = fixture(), c = f.attach(); await tick();
    f.view.controllers[0].dispose = () => { throw new Error("destroyed"); };
    f.view.window.removeEventListener = () => { throw new Error("destroyed"); };
    f.form.data.removeOnLoad = () => { throw new Error("gone"); };
    f.tab.removeTabStateChange = () => { throw new Error("gone"); };
    c.dispose(); await c.refresh(); assert.equal(f.sent.length, 0);
    const g = fixture(), d = g.attach(); await tick(); g.view.window.document.getElementById = () => null;
    d.dispose(); assert.equal(g.dataHooks.size, 0);
});

test("guard rejects unexpected targets before any native execute", async () => {
    const f = fixture(), c = f.attach(); await tick();
    for (const value of [null, {}, { entity: null }, { entity: { entityType: "other", id: A } }, request(B), request("bad")]) {
        await assert.rejects(f.view.calls[0].execute(value));
    }
    assert.equal(f.sent.length, 0); f.state.id = B; await assert.rejects(f.view.calls[0].execute(request())); c.dispose();
});

test("late native responses and response bodies never reach a changed record", async () => {
    const f = fixture(), response = deferred(); const c = f.api.attach(f.form, "WebResource_scope", "credentialing", () => response.promise);
    await tick(); const pending = f.view.calls[0].execute(request()); f.state.id = B;
    response.resolve(f.response); await assert.rejects(pending); c.dispose();
    const g = fixture(), body = deferred(); g.response.json = () => body.promise;
    const d = g.attach(); await tick(); const packet = await g.view.calls[0].execute(request());
    const read = packet.json(); g.state.id = B; body.resolve({ oldRecord: true }); await assert.rejects(read); d.dispose();
    const h = fixture(), e = h.attach(); await tick(); const value = await h.view.calls[0].execute(request());
    e.dispose(); await assert.rejects(value.json());
});

test("HTTP and native errors do not become successful result packets", async () => {
    for (const response of [null, { ok: false, json() {} }, { ok: true, json: null }]) {
        const f = fixture(), c = f.api.attach(f.form, "WebResource_scope", "credentialing", async () => response);
        await tick(); await assert.rejects(f.view.calls[0].execute(request())); c.dispose();
    }
    const f = fixture(), c = f.api.attach(f.form, "WebResource_scope", "credentialing", async () => { throw new Error("native failed"); });
    await tick(); await assert.rejects(f.view.calls[0].execute(request())); c.dispose();
});

test("a new native frame replaces a destroyed one without retaining its listener", async () => {
    const f = fixture(), c = f.attach(); await tick(); const next = frame();
    f.control.getContentWindow = async () => next.window; await c.refresh();
    assert.equal(f.view.listeners.size, 0); assert.equal(next.listeners.size, 1);
    assert.equal(f.view.container.values.length, 0); c.dispose(); assert.equal(next.listeners.size, 0);
});

test("requires a visible containing section on the configured tab", async () => {
    for (const section of [null, { getVisible: null }, { getVisible: () => true, getParent: () => ({ getName: () => "other" }) }]) {
        const f = fixture(); f.control.getParent = () => section;
        assert.throws(f.attach); assert.equal(f.dataHooks.size, 0); assert.equal(f.sent.length, 0);
    }
    const f = fixture(), c = f.attach(); await tick();
    f.state.sectionVisible = false;
    await assert.rejects(f.view.calls[0].execute(request())); assert.equal(f.sent.length, 0);
    await c.refresh(); assert.equal(f.view.container.values.length, 0); c.dispose();
});
