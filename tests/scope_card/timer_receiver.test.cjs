'use strict';
const { test } = require('node:test');
const assert = require('node:assert/strict');
require('../../powerplatform/src/app/credentialing-register/scope-card.js');

test('default native timers retain the browser global receiver',async()=>{
    const originalSet=globalThis.setTimeout, originalClear=globalThis.clearTimeout;
    let scheduled=0, cleared=0;
    globalThis.setTimeout=function(fn,ms){assert.equal(this,globalThis);scheduled++;return originalSet(fn,ms);};
    globalThis.clearTimeout=function(value){assert.equal(this,globalThis);cleared++;return originalClear(value);};
    const doc={createElement(){return {textContent:''};}};
    const host={ownerDocument:doc,children:[],replaceChildren(){this.children=[];},
        appendChild(child){this.children.push(child);},setAttribute(){}};
    try {
        const card=globalThis.CareOpsScopeCard;
        const c=card.mount(host,'10000000-0000-0000-0000-000000000001',async()=>({ok:true,json:async()=>({
            ReadSucceeded:true,ReadDisposition:'recorded-data-only-not-practice-clearance',RootVersion:'9',
            AsOfUtc:'2026-09-24T10:00:00Z',CurrentnessVerified:false,HasEffectiveDecision:false,
            RecordedStatus:'NoEffectiveDecision',LatestSequence:0,SelectedSequence:0,SelectedDecision:null
        })}));
        await new Promise(resolve=>setImmediate(resolve));
        assert.equal(scheduled,1);assert.equal(cleared,1);
        assert(host.children.some(node=>node.textContent==='No effective recorded decision'));
        c.dispose();
    } finally {
        globalThis.setTimeout=originalSet;globalThis.clearTimeout=originalClear;
    }
});
