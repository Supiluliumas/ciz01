import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {runInNewContext} from 'node:vm';
const workflow=JSON.parse(readFileSync(new URL('../../docs/n8n-workflow.json',import.meta.url)));
const node=name=>workflow.nodes.find(node=>node.name===name);
const valid={requestId:'00000000-0000-4000-8000-000000000001',to:'recipient@example.com',subject:'Reviewed subject',body:'Reviewed body'};
const validate=body=>runInNewContext(`(function(){${node('Validate request').parameters.jsCode}})()`,{$input:{first:()=>({json:{body}})}});
test('workflow uses Header Auth, fixed owner sender, and response after SMTP',()=>{
 assert.equal(node('Webhook').parameters.authentication,'headerAuth');
 assert.equal(node('Webhook').parameters.responseMode,'responseNode');
 assert.equal(node('Send Email').parameters.fromEmail,'owner@example.com');
 assert.equal(workflow.connections['Send Email'].main[0][0].node,'Respond to Webhook');
 assert.notEqual(node('Send Email').continueOnFail,true);
 assert.equal(workflow.active,false);
});
test('validator preserves valid reviewed payload',()=>assert.equal(JSON.stringify(validate(valid)[0].json),JSON.stringify(valid)));
for(const [label,patch] of [['recipient',{to:'a@example.com,b@example.com'}],['header newline',{subject:'bad\r\nheader'}],['long subject',{subject:'a'.repeat(201)}],['long body',{body:'a'.repeat(10001)}],['request identifier',{requestId:'not-uuid'}],['missing body',{body:null}]])
 test(`workflow rejects invalid ${label}`,()=>assert.throws(()=>validate({...valid,...patch})));
test('workflow does not save email execution bodies',()=>{
 assert.equal(workflow.settings.saveDataSuccessExecution,'none');
 assert.equal(workflow.settings.saveDataErrorExecution,'none');
 assert.equal(workflow.settings.saveManualExecutions,false);
});
