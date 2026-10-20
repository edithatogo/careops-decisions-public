/* SYNTHETIC fixture only. No user input is used as case data and no network requests are made. */
(() => {
    "use strict";
    const record = "10000000-0000-0000-0000-000000000001";
    const scenarios = Object.freeze({
        recorded: { status: "ScopeRecorded", state: 602170001, verified: true },
        revoked: { status: "Revoked", state: 602170003, verified: false },
        expired: { status: "GrantExpired", state: 602170001, verified: false },
        missing: { status: "ScopeRecorded", state: 602170001, verified: true }
    });
    const selector = document.getElementById("scenario");
    function show() {
        const selected = scenarios[selector.value];
        const raw = { ReadSucceeded: true, RootVersion: "9", AsOfUtc: "2026-09-24T10:00:00Z",
            CurrentnessVerified: selected.verified, RecordedStatus: selected.status, LatestSequence: 1,
            SelectedSequence: 1, HasEffectiveDecision: true, ReadDisposition: "recorded-data-only-not-practice-clearance",
            SelectedDecision: { dft_credentialingdecisionid: "20000000-0000-0000-0000-000000000002", _dft_record_value: record,
                dft_sequence: 1, dft_recordedstate: selected.state, dft_scopeversionref: "SYNTHETIC-SCOPE-V1",
                dft_scopetext: "Synthetic scope for display testing. This is not a practitioner record.",
                dft_conditionstext: "Synthetic condition: retain the documented limitation.",
                dft_supervisiontext: "Synthetic supervision requirement: a named supervisor must be available.",
                dft_validfrom: "2026-09-20T00:00:00Z", dft_validuntil: selector.value === "expired" ? "2026-09-21T00:00:00Z" : "2026-10-20T00:00:00Z",
                dft_recordedat: "2026-09-20T00:00:00Z", dft_effectiveat: "2026-09-20T00:00:00Z" } };
        if (selector.value === "missing") delete raw.SelectedDecision.dft_conditionstext;
        CareOpsScopeCard.bindPage(document, record, async () => ({ ok: true, json: async () => raw }));
    }
    selector.onchange = show;
    show();
})();
