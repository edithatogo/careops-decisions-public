/* Read-only model-driven app web resource. No storage, telemetry, writes or identity claims. */
(() => {
    "use strict";
    const apiName = "dft_ReadCurrentRecordedScopeV1";
    const disposition = "recorded-data-only-not-practice-clearance";
    const labels = Object.freeze({
        NoEffectiveDecision: "No effective recorded decision",
        ScopeRecorded: "Scope recorded",
        GrantNotYetValid: "Recorded grant not yet valid",
        GrantExpired: "Recorded grant expired",
        Suspended: "Recorded suspension",
        Revoked: "Recorded revocation",
        Refused: "Recorded refusal"
    });
    const states = Object.freeze({ ScopeRecorded: 602170001, GrantNotYetValid: 602170001,
        GrantExpired: 602170001, Suspended: 602170002, Revoked: 602170003, Refused: 602170004 });
    const failure = "Recorded scope is unavailable. No decision or scope has been changed.";
    const mounts = new WeakMap();
    function ensure(condition) { if (!condition) throw new Error(failure); }
    function guid(value) {
        ensure(typeof value === "string");
        ensure(/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(value));
        ensure(value !== "00000000-0000-0000-0000-000000000000");
        return value.toLowerCase();
    }
    function utc(value) {
        ensure(typeof value === "string");
        ensure(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?Z$/.test(value));
        const millis = Date.parse(value.slice(0, 19) + "Z");
        ensure(Number.isFinite(millis) && !value.startsWith("0000"));
        ensure(new Date(millis).toISOString().slice(0, 19) === value.slice(0, 19));
        // Keep all seven .NET fractional digits; millisecond rounding can reverse a validity boundary.
        const fraction = value.includes(".") ? value.slice(20, -1).padEnd(7, "0") : "0";
        return BigInt(millis) * 10000n + BigInt(fraction);
    }
    function text(value, maximum, empty) {
        ensure(typeof value === "string");
        ensure(value.length <= maximum && value.isWellFormed());
        ensure(empty || value.trim().length > 0);
        return value;
    }
    function object(value) { ensure(value !== null && typeof value === "object" && !Array.isArray(value)); }
    class ReadRequest {
        constructor(record) { this.entity = Object.freeze({ entityType: "dft_credentialingrecord", id: guid(record) }); }
        getMetadata() {
            return { boundParameter: "entity", operationName: apiName, operationType: 0,
                parameterTypes: { entity: { typeName: "mscrm.dft_credentialingrecord", structuralProperty: 5 } } };
        }
    }
    function parseResponse(raw, record) {
        const id = guid(record);
        object(raw);
        ensure(raw.ReadSucceeded === true && raw.ReadDisposition === disposition);
        ensure(typeof raw.CurrentnessVerified === "boolean" && typeof raw.HasEffectiveDecision === "boolean");
        ensure(typeof raw.RecordedStatus === "string" && Object.hasOwn(labels, raw.RecordedStatus));
        ensure(Number.isInteger(raw.LatestSequence) && raw.LatestSequence >= 0 && raw.LatestSequence <= 256);
        ensure(Number.isInteger(raw.SelectedSequence) && raw.SelectedSequence >= 0 && raw.SelectedSequence <= raw.LatestSequence);
        ensure(raw.HasEffectiveDecision === (raw.SelectedSequence > 0));
        ensure(typeof raw.RootVersion === "string" && /^[1-9][0-9]{0,19}$/.test(raw.RootVersion));
        ensure(BigInt(raw.RootVersion) <= 18446744073709551615n);
        const at = utc(raw.AsOfUtc);
        let details = null;
        if (!raw.HasEffectiveDecision) {
            ensure(raw.RecordedStatus === "NoEffectiveDecision" && raw.SelectedDecision === null);
        } else {
            const row = raw.SelectedDecision;
            object(row);
            ensure(Object.hasOwn(states, raw.RecordedStatus));
            guid(row.dft_credentialingdecisionid);
            ensure(guid(row._dft_record_value) === id);
            ensure(row.dft_sequence === raw.SelectedSequence && row.dft_recordedstate === states[raw.RecordedStatus]);
            const from = utc(row.dft_validfrom), until = utc(row.dft_validuntil);
            ensure(from < until && utc(row.dft_recordedat) <= at && utc(row.dft_effectiveat) <= at);
            // Verify the returned display category, not a new local credentialing decision.
            if (raw.RecordedStatus === "ScopeRecorded") ensure(from <= at && at < until);
            if (raw.RecordedStatus === "GrantNotYetValid") ensure(at < from);
            if (raw.RecordedStatus === "GrantExpired") ensure(at >= until);
            details = Object.freeze({ scopeVersion: text(row.dft_scopeversionref, 256, false),
                scope: text(row.dft_scopetext, 32768, false),
                conditions: text(row.dft_conditionstext, 32768, true),
                supervision: text(row.dft_supervisiontext, 32768, true),
                validFrom: row.dft_validfrom, validUntil: row.dft_validuntil });
        }
        return Object.freeze({ record: id, version: raw.RootVersion, asOf: raw.AsOfUtc,
            status: labels[raw.RecordedStatus], verified: raw.CurrentnessVerified,
            selectedSequence: raw.SelectedSequence, latestSequence: raw.LatestSequence, details });
    }
    function render(container, state) {
        const document = container.ownerDocument;
        container.replaceChildren();
        container.setAttribute("aria-live", "polite");
        container.setAttribute("aria-atomic", "true");
        function line(tag, value) {
            const item = document.createElement(tag); item.textContent = value; container.appendChild(item); return item;
        }
        line("h2", "Recorded scope snapshot");
        line("p", "Recorded data only. Not permission to practise.");
        if (state.kind !== "ready") {
            line("p", state.kind === "loading" ? "Reading the register…" : failure);
            return;
        }
        const view = state.view;
        line("h3", view.status);
        line("p", view.verified ? "Source verification covered this read time." : "Source verification is not current or is unavailable.");
        line("p", "As of " + view.asOf + " (UTC). This snapshot can change after retrieval.");
        if (view.details !== null) {
            line("h3", "Scope of clinical practice"); line("p", view.details.scope);
            line("h3", "Conditions and limitations");
            line("p", view.details.conditions || "No conditions supplied in this recorded decision.");
            line("h3", "Supervision");
            line("p", view.details.supervision || "No supervision requirements supplied in this recorded decision.");
            line("p", "Recorded validity: " + view.details.validFrom + " to " + view.details.validUntil + " (exclusive end, UTC).");
        }
    }
    function mount(container, record, execute, timers = { set: globalThis.setTimeout.bind(globalThis), clear: globalThis.clearTimeout.bind(globalThis) }) {
        const previous = mounts.get(container);
        if (previous) previous.dispose();
        // A failed rebind must never leave the previous practitioner's snapshot on screen.
        render(container, { kind: "unavailable" });
        const id = guid(record);
        ensure(typeof execute === "function");
        let generation = 0, disposed = false, timer = null;
        function cancelTimer() { if (timer !== null) { timers.clear(timer); timer = null; } }
        async function refresh() {
            if (disposed) return;
            const requestGeneration = ++generation;
            cancelTimer(); render(container, { kind: "loading" });
            timer = timers.set(() => {
                if (!disposed && generation === requestGeneration) {
                    timer = null; generation++; render(container, { kind: "unavailable" });
                }
            }, 15000);
            try {
                const response = await execute(new ReadRequest(id));
                ensure(response && response.ok === true && typeof response.json === "function");
                const view = parseResponse(await response.json(), id);
                if (!disposed && generation === requestGeneration) {
                    cancelTimer(); render(container, { kind: "ready", view });
                }
            } catch {
                if (!disposed && generation === requestGeneration) {
                    cancelTimer(); render(container, { kind: "unavailable" });
                }
            }
        }
        function dispose() {
            disposed = true; generation++; cancelTimer();
            if (mounts.get(container) === controller) container.replaceChildren();
        }
        const controller = Object.freeze({ refresh, dispose });
        mounts.set(container, controller);
        void refresh();
        return controller;
    }
    function bindPage(document, record, execute) {
        const container = document.getElementById("scope-card"), button = document.getElementById("scope-refresh");
        ensure(container !== null && button !== null);
        button.disabled = true; button.onclick = null;
        const controller = mount(container, record, execute);
        button.onclick = controller.refresh;
        button.disabled = false;
        return controller;
    }
    globalThis.CareOpsScopeCard = Object.freeze({ ReadRequest, parseResponse, render, mount, bindPage });
})();
