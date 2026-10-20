/* Unified Interface form library. Binds the read-only card; never writes or grants authority. */
(() => {
    "use strict";
    const table = "dft_credentialingrecord";
    const apiName = "dft_ReadCurrentRecordedScopeV1";
    const sessions = new WeakMap();
    const failure = "Recorded scope binding is unavailable. No decision or scope has been changed.";
    function ensure(condition) { if (!condition) throw new Error(failure); }
    function identifier(value) {
        if (typeof value !== "string") return null;
        const text = /^\{[0-9a-f-]+\}$/i.test(value) ? value.slice(1, -1) : value;
        if (!/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(text) ||
            text === "00000000-0000-0000-0000-000000000000") return null;
        return text.toLowerCase();
    }
    class ReadRequest {
        constructor(id) { this.entity = Object.freeze({ entityType: table, id }); Object.freeze(this); }
        getMetadata() {
            return { boundParameter: "entity", operationName: apiName, operationType: 0,
                parameterTypes: { entity: { typeName: "mscrm." + table, structuralProperty: 5 } } };
        }
    }
    function attach(form, controlName, tabName, execute) {
        const previous = sessions.get(form);
        if (previous) previous.dispose();
        ensure(form !== null && typeof form === "object");
        ensure(typeof controlName === "string" && controlName.length > 0 && controlName.length <= 128);
        ensure(typeof tabName === "string" && tabName.length > 0 && tabName.length <= 128);
        ensure(typeof execute === "function");
        const control = form.getControl(controlName), tab = form.ui.tabs.get(tabName);
        ensure(control && control.getControlType() === "webresource" && typeof control.getContentWindow === "function");
        ensure(tab && typeof tab.addTabStateChange === "function" && typeof tab.removeTabStateChange === "function");
        const section = control.getParent();
        ensure(section && typeof section.getVisible === "function" && section.getParent().getName() === tabName);
        ensure(typeof form.data.addOnLoad === "function" && typeof form.data.removeOnLoad === "function");
        let disposed = false, generation = 0, child = null, mounted = null;
        let dataHook = false, tabHook = false;
        function releaseFrame() {
            const oldChild = child, oldMount = mounted;
            child = null; mounted = null;
            try { if (oldMount) oldMount.dispose(); } catch { /* Finish cleanup even if the old frame was destroyed. */ }
            if (oldChild) {
                try {
                    oldChild.removeEventListener("pagehide", frameHidden);
                    const document = oldChild.document;
                    const container = document.getElementById("scope-card"), button = document.getElementById("scope-refresh");
                    if (container) container.replaceChildren();
                    if (button) { button.disabled = true; button.onclick = null; }
                } catch { /* A destroyed/cross-origin frame is not a source of trusted data. */ }
            }
        }
        function selectedRecord() {
            if (control.getVisible() !== true || section.getVisible() !== true || tab.getVisible() !== true || tab.getDisplayState() !== "expanded" ||
                form.data.entity.getEntityName() !== table) return null;
            return identifier(form.data.entity.getId());
        }
        function current(run, id) {
            ensure(!disposed && generation === run && selectedRecord() === id);
        }
        function frameHidden() { generation++; releaseFrame(); }
        async function refresh() {
            if (disposed) return;
            const run = ++generation;
            releaseFrame();
            try {
                const id = selectedRecord();
                if (id === null) return;
                const window = await control.getContentWindow();
                if (disposed || generation !== run) return;
                if (selectedRecord() !== id) return;
                ensure(window && window.document && typeof window.addEventListener === "function");
                child = window;
                ensure(window.CareOpsScopeCard && typeof window.CareOpsScopeCard.bindPage === "function");
                window.addEventListener("pagehide", frameHidden);
                const guardedExecute = async request => {
                    current(run, id);
                    ensure(request && request.entity && request.entity.entityType === table && identifier(request.entity.id) === id);
                    // Construct our own fixed request. Never forward a supplied action or caller identity.
                    const response = await execute(new ReadRequest(id));
                    current(run, id);
                    ensure(response && response.ok === true && typeof response.json === "function");
                    return Object.freeze({ ok: true, json: async () => {
                        current(run, id);
                        const value = await response.json();
                        current(run, id);
                        return value;
                    } });
                };
                mounted = window.CareOpsScopeCard.bindPage(window.document, id, guardedExecute);
                ensure(mounted && typeof mounted.dispose === "function" && typeof mounted.refresh === "function");
            } catch {
                if (!disposed && generation === run) { generation++; releaseFrame(); }
            }
        }
        function dispose() {
            if (disposed) return;
            disposed = true; generation++; releaseFrame();
            if (dataHook) { try { form.data.removeOnLoad(refresh); } catch { /* Continue removing the other handler. */ } }
            if (tabHook) { try { tab.removeTabStateChange(refresh); } catch { /* Disposed sessions cannot make requests. */ } }
            if (sessions.get(form) === controller) sessions.delete(form);
        }
        const controller = Object.freeze({ refresh, dispose });
        sessions.set(form, controller);
        try {
            dataHook = true; form.data.addOnLoad(refresh);
            tabHook = true; tab.addTabStateChange(refresh);
        } catch { dispose(); throw new Error(failure); }
        void refresh();
        return controller;
    }
    function onLoad(executionContext, controlName, tabName) {
        try {
            const form = executionContext.getFormContext();
            const online = globalThis.Xrm?.WebApi?.online;
            // The native session owns authentication; there is no app ID, token, name/email or HTTP fallback.
            const execute = typeof online?.execute === "function" ? request => online.execute(request) : null;
            return attach(form, controlName, tabName, execute);
        } catch { return null; }
    }
    globalThis.CareOpsScopeForm = Object.freeze({ attach, onLoad });
})();
