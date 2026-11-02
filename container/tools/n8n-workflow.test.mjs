import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {runInNewContext} from 'node:vm';
const workflow=JSON.parse(readFileSync(new URL('../../docs/n8n-workflow.json',import.meta.url)));
const node=name=>workflow.nodes.find(node=>node.name===name);
const valid={requestId:'00000000-0000-4000-8000-000000000001',to:'recipient@example.com',subject:'Reviewed subject',body:'Reviewed body'};
const validate=body=>runInNewContext(`(function(){${node('Kontrola požadavku').parameters.jsCode}})()`,{$input:{first:()=>({json:{body}})}});
const webhook='Webhook z aplikace ciz01',send='Odeslat e-mail (Outlook)',respond='Potvrdit odeslání aplikaci';
test('workflow uses Header Auth, Outlook send from credential owner, and response after send',()=>{
 assert.equal(node(webhook).parameters.authentication,'headerAuth');
 assert.equal(node(webhook).parameters.responseMode,'responseNode');
 assert.equal(node(webhook).parameters.path,'ciz01-mailer');
 assert.equal(node(send).type,'n8n-nodes-base.microsoftOutlook');
 assert.equal(node(send).parameters.operation,'send');
 assert.equal(node(send).parameters.toRecipients,'={{ $json.to }}');
 assert.equal(node(send).parameters.additionalFields.bodyContentType,'Text');
 assert.equal(workflow.connections[send].main[0][0].node,respond);
 assert.match(node(respond).parameters.responseBody,new RegExp(`\\$\\('${webhook}'\\)`));
 assert.notEqual(node(send).continueOnFail,true);
 assert.equal(workflow.active,false);
});
test('workflow notifies owner on failure without email content',()=>{
 assert.equal(workflow.connections['Při chybě workflow'].main[0][0].node,'Upozornit vlastníka na chybu');
 const notify=node('Upozornit vlastníka na chybu').parameters;
 assert.equal(notify.toRecipients,'martin.cizkovsky@alza.cz');
 assert.doesNotMatch(notify.subject+notify.bodyContent,/\.body|\.subject\b|\.to\b/);
});
test('validator preserves valid reviewed payload',()=>assert.equal(JSON.stringify(validate(valid)[0].json),JSON.stringify(valid)));
for(const [label,patch] of [['recipient',{to:'a@example.com,b@example.com'}],['recipient list',{to:'a,b@example.com'}],['recipient separator',{to:'a;b@example.com'}],['header newline',{subject:'bad\r\nheader'}],['long subject',{subject:'a'.repeat(201)}],['long body',{body:'a'.repeat(10001)}],['request identifier',{requestId:'not-uuid'}],['missing body',{body:null}]])
 test(`workflow rejects invalid ${label}`,()=>assert.throws(()=>validate({...valid,...patch})));
test('workflow does not save email execution bodies',()=>{
 assert.equal(workflow.settings.saveDataSuccessExecution,'none');
 assert.equal(workflow.settings.saveDataErrorExecution,'none');
 assert.equal(workflow.settings.saveManualExecutions,false);
});
