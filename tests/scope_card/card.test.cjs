'use strict';
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
require('../../powerplatform/src/app/credentialing-register/scope-card.js');
const card = globalThis.CareOpsScopeCard;
const id = '10000000-0000-0000-0000-000000000001';
const decision = '20000000-0000-0000-0000-000000000002';
const disposition = 'recorded-data-only-not-practice-clearance';
function fixture() {
    return { ReadSucceeded: true, RootVersion: '9', AsOfUtc: '2026-09-24T10:00:00.0000000Z',
        CurrentnessVerified: true, RecordedStatus: 'ScopeRecorded', LatestSequence: 1, SelectedSequence: 1,
        HasEffectiveDecision: true, ReadDisposition: disposition, SelectedDecision: {
            dft_credentialingdecisionid: decision, _dft_record_value: id, dft_sequence: 1,
            dft_recordedstate: 602170001, dft_scopeversionref: 'SYNTHETIC-version',
            dft_scopetext: 'Synthetic scope', dft_conditionstext: 'Synthetic condition', dft_supervisiontext: 'Named supervision',
            dft_validfrom: '2026-09-20T00:00:00Z', dft_validuntil: '2026-10-20T00:00:00Z',
            dft_effectiveat: '2026-09-20T00:00:00Z', dft_recordedat: '2026-09-20T00:00:00Z'
        }};
}
function empty() { return { ...fixture(), HasEffectiveDecision: false, RecordedStatus: 'NoEffectiveDecision', SelectedSequence: 0, SelectedDecision: null }; }
function parse(raw) { return card.parseResponse(raw, id); }
function reject(raw) { assert.throws(() => parse(raw), /^Error: Recorded scope is unavailable/); }
class Element {
    constructor(tag, doc) { this.tagName = tag; this.ownerDocument = doc; this.children = []; this.attributes = {}; this.textContent = ''; }
    replaceChildren(...children) { this.children = children; this.textContent = ''; }
    appendChild(child) { this.children.push(child); return child; }
    setAttribute(k, v) { this.attributes[k] = v; }
    text() { return this.textContent + this.children.map(c => c.text()).join('\n'); }
}
function root() { const doc = { createElement(tag) { return new Element(tag, doc); } }; return new Element('section', doc); }
function timers() {
    let next = 0;
    const pending = new Map(), old = new Map();
    return { pending, old, set(fn, ms) { assert.equal(ms, 15000); const key = ++next; pending.set(key, fn); old.set(key, fn); return key; },
        clear(key) { pending.delete(key); }, fire(key) { const fn = pending.get(key); pending.delete(key); fn(); } };
}
function deferred() { let resolve, reject; const promise = new Promise((a,b) => { resolve=a; reject=b; }); return { promise, resolve, reject }; }
function response(raw = fixture()) { return { ok: true, async json() { return raw; } }; }
const flush = () => new Promise(resolve => setImmediate(resolve));

test('one supported bound read has no actor/time/privilege inputs', () => {
    const request = new card.ReadRequest(id.toUpperCase());
    assert.deepEqual(Object.keys(request), ['entity']);
    assert.deepEqual(request.entity, { entityType: 'dft_credentialingrecord', id });
    assert.equal(Object.hasOwn(request, 'getMetadata'), false);
    assert.deepEqual(request.getMetadata(), { boundParameter: 'entity', operationName: 'dft_ReadCurrentRecordedScopeV1', operationType: 0,
        parameterTypes: { entity: { typeName: 'mscrm.dft_credentialingrecord', structuralProperty: 5 } } });
    assert(Object.isFrozen(request.entity));
    for (const bad of [null, 1, {}, '', '{' + id + '}', '00000000-0000-0000-0000-000000000000', 'x' + id]) {
        assert.throws(() => new card.ReadRequest(bad));
    }
});
test('only minimum fields are frozen; private/extra metadata never leaves the boundary', () => {
    const raw = fixture(); raw.Actor = 'private'; raw.SelectedDecision.dft_actorref='private';
    raw.SelectedDecision['@odata.type']='#Microsoft.Dynamics.CRM.dft_credentialingdecision';
    const view = parse(raw); assert(Object.isFrozen(view)); assert(Object.isFrozen(view.details));
    assert.equal(view.record, id); assert.equal(view.status, 'Scope recorded'); assert.equal(view.verified, true);
    assert.equal(view.details.scope, 'Synthetic scope'); assert.equal(view.details.scopeVersion, 'SYNTHETIC-version');
    raw.SelectedDecision.dft_scopetext='changed'; assert.equal(view.details.scope, 'Synthetic scope');
    assert.equal(JSON.stringify(view).includes('private'), false);
    assert.equal(parse({ ...raw, RootVersion: '18446744073709551615' }).version, '18446744073709551615');
});
test('empty/future history is a read result, not a grant', () => {
    const view = parse(empty()); assert.equal(view.details, null); assert.equal(view.selectedSequence, 0); assert.equal(view.latestSequence,1);
    assert.equal(parse({...empty(), LatestSequence:0}).details, null);
    reject({...empty(), SelectedDecision: undefined}); reject({...empty(), RecordedStatus:'ScopeRecorded'});
    reject({...fixture(), RecordedStatus:'NoEffectiveDecision'});
});
test('all recorded states retain restrictions and currentness separately', () => {
    for (const [status, label, state] of [['Suspended','Recorded suspension',602170002],['Revoked','Recorded revocation',602170003],['Refused','Recorded refusal',602170004]]) {
        const raw=fixture(); raw.RecordedStatus=status; raw.CurrentnessVerified=false; raw.SelectedDecision.dft_recordedstate=state;
        // An expired validity interval does not hide an adverse recorded decision.
        raw.SelectedDecision.dft_validuntil='2026-09-21T00:00:00Z';
        const view=parse(raw); assert.equal(view.status,label); assert.equal(view.verified,false); assert.equal(view.details.conditions,'Synthetic condition');
    }
});
test('UTC boundaries preserve 100ns precision without rounding away restrictions', () => {
    let raw=fixture(); raw.AsOfUtc='2026-09-24T10:00:00.0000001Z';
    raw.SelectedDecision.dft_validfrom='2026-09-24T10:00:00.0000001Z'; raw.SelectedDecision.dft_validuntil='2026-09-24T10:00:00.0000002Z';
    assert.equal(parse(raw).status,'Scope recorded');
    raw.AsOfUtc=raw.SelectedDecision.dft_validuntil; reject(raw); raw.RecordedStatus='GrantExpired';
    assert.equal(parse(raw).status,'Recorded grant expired');
    raw.AsOfUtc='2026-09-24T10:00:00Z'; raw.RecordedStatus='GrantNotYetValid';
    assert.equal(parse(raw).status,'Recorded grant not yet valid');
    raw.AsOfUtc='2026-09-24T10:00:00.0000001Z'; reject(raw);
    raw.RecordedStatus='GrantExpired'; reject(raw);
    raw=fixture(); raw.AsOfUtc='2026-09-24T10:00:00.1Z'; assert.equal(parse(raw).asOf,raw.AsOfUtc);
});
test('malformed timestamps and impossible Gregorian dates do not normalize silently', () => {
    for (const at of [null, 0, '', '2026-09-24', '2026-09-24T10:00:00+00:00', '2026-09-24T10:00:00.00000001Z',
        '2026-13-24T10:00:00Z', '2026-02-30T10:00:00Z', '0000-01-01T00:00:00Z', '2026-09-24T24:00:00Z']) reject({...fixture(),AsOfUtc:at});
    const raw=fixture(); raw.SelectedDecision.dft_recordedat='2026-09-24T10:00:00.0000001Z'; reject(raw);
    raw.SelectedDecision.dft_recordedat='2026-09-20T00:00:00Z';raw.SelectedDecision.dft_effectiveat='2026-09-24T10:00:00.0000001Z'; reject(raw);
    raw.SelectedDecision.dft_effectiveat='2026-09-20T00:00:00Z';raw.SelectedDecision.dft_validuntil=raw.SelectedDecision.dft_validfrom; reject(raw);
});
test('success flags, status, version and sequence require exact types and bounds', () => {
    const variants = { ReadSucceeded:[false,null,1,'true'], ReadDisposition:[null,'approved'],
        CurrentnessVerified:[null,0,'true'],HasEffectiveDecision:[null,0,'true'], RecordedStatus:['Unverified','toString',null,{}],
        LatestSequence:[-1,257,1.5,'1',0],SelectedSequence:[-1,2,1.5,'1',0],RootVersion:[null,1,'','0','01','+1',' 1','1.0','18446744073709551616','184467440737095516150'] };
    for(const [key,values] of Object.entries(variants)) for(const value of values) reject({...fixture(),[key]:value});
    for(const bad of [null,1,'response',[]]) reject(bad);
    assert.equal(parse({...fixture(),LatestSequence:256}).latestSequence,256);
});
test('target, decision and state must agree; missing or coerced values are not guessed', () => {
    const variants = { dft_credentialingdecisionid:['',null],_dft_record_value:[decision,null],dft_sequence:['1',2], dft_recordedstate:['602170001',602170003] };
    for(const [key,values] of Object.entries(variants)) for(const value of values) { const raw=fixture();raw.SelectedDecision[key]=value; reject(raw); }
    for(const bad of [null,[],1]) reject({...fixture(),SelectedDecision:bad});
});
test('required text is bounded and well-formed; absent restrictions do not mean none', () => {
    for(const [field,max,required] of [['dft_scopeversionref',256,true],['dft_scopetext',32768,true],['dft_conditionstext',32768,false],['dft_supervisiontext',32768,false]]) {
        for(const value of [null,1,undefined,'x'.repeat(max+1),'\ud800']) { const raw=fixture(); raw.SelectedDecision[field]=value; reject(raw); }
        const raw=fixture(); raw.SelectedDecision[field]='x'.repeat(max); assert.notEqual(parse(raw).details,null);
        raw.SelectedDecision[field]=''; if(required) reject(raw); else assert.notEqual(parse(raw).details,null);
    }
});
test('render uses text nodes with explicit time/currentness and no raw fault or markup execution', () => {
    const host=root(); const raw=fixture(); raw.CurrentnessVerified=false;
    raw.SelectedDecision.dft_scopetext='<script>not executable</script>'; raw.SelectedDecision.dft_conditionstext=''; raw.SelectedDecision.dft_supervisiontext='';
    card.render(host,{kind:'ready',view:parse(raw)});
    assert(host.text().includes('<script>not executable</script>')); assert(host.text().includes('Source verification is not current'));
    assert(host.text().includes('No conditions supplied')); assert(host.text().includes('No supervision requirements supplied'));
    assert.equal(host.attributes['aria-live'],'polite'); assert.equal(host.attributes['aria-atomic'],'true');
    assert.equal(host.children.some(c=>c.tagName==='script'),false);
    card.render(host,{kind:'ready',view:parse(empty())}); assert(!host.text().includes('Conditions and limitations'));
    card.render(host,{kind:'unavailable'}); assert(host.text().includes('unavailable')); assert(!host.text().includes('<script>'));
});
test('mount loads once using supplied authenticated executor and shows only validated output',async () => {
    const host=root(), t=timers(); let calls=0;
    const c=card.mount(host,id,async request=>{calls++;assert.equal(request.entity.id,id);return response();},t);
    assert(host.text().includes('Reading')); await flush(); assert.equal(calls,1); assert(host.text().includes('Source verification covered'));
    assert.equal(t.pending.size,0); assert(Object.isFrozen(c)); c.dispose(); assert.equal(host.text(),'');
    await c.refresh();assert.equal(calls,1);
});
test('refresh clears old data immediately and failure never reveals private error content',async () => {
    const host=root(),t=timers();let fail=false;
    const c=card.mount(host,id,()=>{if(fail) throw new Error('PRIVATE-RECORD'); return response();},t);
    await flush();assert(host.text().includes('Synthetic scope')); fail=true;
    await c.refresh();assert(!host.text().includes('Synthetic scope'));assert(!host.text().includes('PRIVATE-RECORD'));assert(host.text().includes('unavailable')); c.dispose();
});
test('bad HTTP, decoder and payload responses cannot become displayed success',async () => {
    for(const returned of [null, {ok:false}, {ok:true}, {ok:'true',json(){}}, {ok:true,json:async()=>{throw new Error('PRIVATE');}},response({})]) {
        const host=root(),t=timers();const c=card.mount(host,id,async()=>returned,t);
        await flush();assert(host.text().includes('unavailable'));assert(!host.text().includes('PRIVATE'));assert.equal(t.pending.size,0);c.dispose();
    }
});
test('latest refresh wins over late success and stale rejection',async () => {
    for(const rejectOld of [false,true]) {
        const host=root(),t=timers(),old=deferred();let count=0;
        const c=card.mount(host,id,()=>++count===1?old.promise:Promise.resolve(response(empty())),t);
        await c.refresh();assert(host.text().includes('No effective recorded decision'));
        if(rejectOld)old.reject(new Error('OLD-ERROR'));else old.resolve(response());
        await flush();assert(host.text().includes('No effective recorded decision'));assert(!host.text().includes('Synthetic scope'));c.dispose();
    }
});
test('timeout invalidates display generation but does not claim transport cancellation',async () => {
    const host=root(),t=timers(),pending=deferred();const c=card.mount(host,id,()=>pending.promise,t);
    t.fire(1);assert(host.text().includes('unavailable'));pending.resolve(response());await flush();assert(host.text().includes('unavailable'));c.dispose();
});
test('cancelled or disposed callbacks cannot invalidate another read timer',async () => {
    const host=root(),t=timers(),a=deferred(),b=deferred();let n=0;
    const c=card.mount(host,id,()=>++n===1?a.promise:b.promise,t);
    void c.refresh();t.old.get(1)();assert(t.pending.has(2)); c.dispose();assert.equal(t.pending.size,0);t.old.get(2)();
    a.resolve(response());b.reject(new Error('disposed'));await flush();assert.equal(host.text(),'');
});
test('remount disposes the old record and ignores its later result',async () => {
    const host=root(),t=timers(),pending=deferred();const old=card.mount(host,id,()=>pending.promise,t);
    const current=card.mount(host,decision,async()=>response({...empty()}),t);await flush();
    pending.resolve(response());await flush();assert(host.text().includes('No effective recorded decision'));await old.refresh();current.dispose();
});
test('invalid rebinding removes the previously displayed record before validation',async () => {
    const host=root(),t=timers();const old=card.mount(host,id,async()=>response(),t);await flush();
    assert.throws(()=>card.mount(host,'invalid',()=>response(),t));assert(!host.text().includes('Synthetic scope'));assert(host.text().includes('unavailable'));
    assert.throws(()=>card.mount(host,id,null,t));await old.refresh();assert(host.text().includes('unavailable'));
});
test('default browser timers are supported and cleaned up on success',async () => {
    const host=root(),c=card.mount(host,id,async()=>response());await flush();assert(host.text().includes('Synthetic scope'));c.dispose();
});
test('source contains no persistence, telemetry, HTML execution or arbitrary network mechanism',()=>{
    const source=fs.readFileSync('powerplatform/src/app/credentialing-register/scope-card.js','utf8');
    for(const bad of [/innerHTML/,/eval\(/,/localStorage/,/sessionStorage/,/fetch\(/,/XMLHttpRequest/,/console\./,/document\.cookie/])assert(!bad.test(source));
});
test('page binding keeps refresh outside the live region and replaces rather than stacks handlers',async () => {
    const host=root(),button={disabled:true,onclick:null}; let calls=0;
    const doc={getElementById(name){return name==='scope-card'?host:button;}};
    const c=card.bindPage(doc,id,async()=>{calls++;return response();}); await flush();
    assert.equal(button.disabled,false); await button.onclick(); assert.equal(calls,2);
    const d=card.bindPage(doc,id,async()=>response(empty()));await flush();await button.onclick();
    assert.equal(calls,2);assert(host.text().includes('No effective recorded decision')); c.dispose();
    assert(host.text().includes('No effective recorded decision'));d.dispose();
});
test('unbound or invalid page cannot keep an old refresh handler or old record',async()=>{
    for(const missing of ['scope-card','scope-refresh']) {
        assert.throws(()=>card.bindPage({getElementById(name){return name===missing?null:{};}},id,()=>response()));
    }
    const host=root(),button={disabled:false,onclick:()=>{throw new Error('old');}};
    const doc={getElementById(name){return name==='scope-card'?host:button;}};
    assert.throws(()=>card.bindPage(doc,'invalid',()=>response()));assert.equal(button.onclick,null);assert.equal(button.disabled,true);
});
