'use strict';
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
test('every fixed preview scenario uses the real card without data entry or network access',async()=>{
    const selector = { value:'recorded',onchange:null },calls=[];
    globalThis.document={getElementById(name){assert.equal(name,'scenario');return selector;}};
    globalThis.CareOpsScopeCard={bindPage(doc,id,execute){assert.equal(doc,globalThis.document);calls.push({id,execute});}};
    require('../../powerplatform/src/app/credentialing-register/preview.js');
    for(const scenario of ['recorded','revoked','expired','missing']) {
        selector.value=scenario;selector.onchange();const current=calls.at(-1);const reply=await current.execute();const raw=await reply.json();
        assert.equal(reply.ok,true);assert.equal(current.id,'10000000-0000-0000-0000-000000000001');
        assert.equal(raw.ReadDisposition,'recorded-data-only-not-practice-clearance');assert(raw.SelectedDecision.dft_scopetext.startsWith('Synthetic'));
        if(scenario==='revoked'){assert.equal(raw.RecordedStatus,'Revoked');assert.equal(raw.CurrentnessVerified,false);}
        if(scenario==='expired')assert.equal(raw.SelectedDecision.dft_validuntil,'2026-09-21T00:00:00Z');
        if(scenario==='missing')assert.equal(Object.hasOwn(raw.SelectedDecision,'dft_conditionstext'),false);
    }
    assert.equal(calls.length,5);
    const html=fs.readFileSync('powerplatform/src/app/credentialing-register/preview.html','utf8');
    assert(html.includes("connect-src 'none'"));assert(!html.includes('type="file"'));assert(html.includes('SYNTHETIC PREVIEW'));
    delete globalThis.document;delete globalThis.CareOpsScopeCard;
});
