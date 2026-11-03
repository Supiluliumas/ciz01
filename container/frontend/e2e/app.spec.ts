import {test,expect} from '@playwright/test';

test('draft, edit, review, and simulate delivery',async({page})=>{
 await page.goto('/');
 await expect(page.getByRole('heading',{name:'Napiš méně. Řekni víc.'})).toBeVisible();
 await expect(page.getByText('Lokální ukázka.',{exact:true})).toBeVisible();
 await page.getByLabel('Příjemce',{exact:false}).fill('test@example.com');
 await page.getByRole('button',{name:'Domluvit schůzku'}).click();
 await page.getByRole('button',{name:'Vytvořit ukázkový návrh'}).click();
 await expect(page.getByLabel('Předmět',{exact:true})).toBeVisible();
 await page.getByLabel('Předmět',{exact:true}).fill('Schůzka — test');
 await page.getByRole('button',{name:'Zkontrolovat a odeslat'}).click();
 await expect(page.getByRole('dialog')).toBeVisible();
 await expect(page.getByRole('dialog').getByText('test@example.com',{exact:true})).toBeVisible();
 await page.getByRole('button',{name:'Zpět k úpravám'}).click();
 await expect(page.getByRole('dialog')).not.toBeVisible();
 await page.getByRole('button',{name:'Zkontrolovat a odeslat'}).click();
 await page.getByRole('button',{name:'Potvrdit ukázkové odeslání'}).click();
 await expect(page.getByRole('status')).toContainText('Žádný skutečný e-mail nebyl odeslán');
 await expect(page.getByRole('button',{name:'Zkontrolovat a odeslat'})).toBeDisabled();
});
test('admin panel and protected final administrator',async({page})=>{
 await page.goto('/admin');
 await expect(page.getByRole('heading',{name:'Správa přístupu.'})).toBeVisible();
 await expect(page.getByRole('button',{name:'Odebrat demo@alza.cz'})).toBeDisabled();
 await page.getByLabel('Firemní e-mail').fill('e2e@alza.cz');
 await page.getByRole('button',{name:'Přidat přístup'}).click();
 await expect(page.getByText('e2e@alza.cz',{exact:true})).toBeVisible();
});
test('read-only sample table',async({page})=>{
 await page.goto('/test-data');
 await expect(page.getByRole('table')).not.toBeVisible();
 await page.getByRole('button',{name:'Načíst testovací data'}).click();
 await expect(page.getByRole('table')).toBeVisible();
 await expect(page.getByText('Ukázková data · BigQuery se nevolá')).toBeVisible();
 await expect(page.locator('tbody tr')).toHaveCount(3);
});
test('desktop screenshot and no horizontal overflow',async({page})=>{
 await page.setViewportSize({width:1440,height:1050});await page.goto('/');
 await expect(page.getByRole('heading',{name:'Napiš méně. Řekni víc.'})).toBeVisible();
 expect(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth)).toBe(true);
 await page.screenshot({path:'../../docs/screenshots/desktop.png',fullPage:true});
});
test('mobile navigation and composer fit',async({page})=>{
 await page.setViewportSize({width:390,height:844});await page.goto('/');
 await expect(page.getByLabel('Příjemce',{exact:false})).toBeVisible();
 expect(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth)).toBe(true);
 await page.screenshot({path:'../../docs/screenshots/mobile.png',fullPage:true});
 await page.getByRole('link',{name:'Testovací data'}).click();
 await expect(page.getByRole('button',{name:'Načíst testovací data'})).toBeVisible();
});

test('drafting error preserves inputs and offers another attempt',async({page})=>{
 await page.route('**/api/drafts',route=>route.fulfill({status:502,json:{message:'AI služba je dočasně nedostupná.'}}));
 await page.goto('/');await page.getByLabel('Příjemce',{exact:false}).fill('test@example.com');
 await page.getByRole('button',{name:'Domluvit schůzku'}).click();
 await page.getByRole('button',{name:'Vytvořit ukázkový návrh'}).click();
 await expect(page.getByRole('alert')).toContainText('AI služba je dočasně nedostupná');
 await expect(page.getByLabel('Příjemce',{exact:false})).toHaveValue('test@example.com');
 await expect(page.getByRole('button',{name:'Vytvořit ukázkový návrh'})).toBeEnabled();
});
test('uncertain sending is blocked even after editing the draft',async({page})=>{
 let calls=0;await page.route('**/api/send',route=>{calls++;return route.fulfill({status:504,json:{message:'Odeslání nemá potvrzený výsledek.'}});});
 await page.goto('/');await page.getByLabel('Příjemce',{exact:false}).fill('test@example.com');
 await page.getByRole('button',{name:'Domluvit schůzku'}).click();
 await page.getByRole('button',{name:'Vytvořit ukázkový návrh'}).click();
 await page.getByRole('button',{name:'Zkontrolovat a odeslat'}).click();
 await page.getByRole('button',{name:'Potvrdit ukázkové odeslání'}).click();
 await expect(page.getByRole('status')).toContainText('Výsledek odeslání je nejistý');
 await page.getByLabel('Předmět',{exact:true}).fill('Edited after timeout');
 await expect(page.getByRole('button',{name:'Zkontrolovat a odeslat'})).toBeDisabled();
 expect(calls).toBe(1);
});
test('blocked user sees access error instead of composer',async({page})=>{
 await page.route('**/api/me',route=>route.fulfill({status:403,json:{message:'Nemáš přístup. Požádej administrátora.'}}));
 await page.goto('/');await expect(page.getByRole('alert')).toContainText('Nemáš přístup');
 await expect(page.getByLabel('Příjemce',{exact:false})).not.toBeVisible();
});
test('empty supplied table has a clear empty state',async({page})=>{
 await page.route('**/api/test-data',route=>route.fulfill({json:{columns:['id'],rows:[],simulated:false,source:'test.table'}}));
 await page.goto('/test-data');await page.getByRole('button',{name:'Načíst testovací data'}).click();
 await expect(page.getByText('Testovací tabulka je prázdná.')).toBeVisible();
});
test('unconfigured integrations show an explicit state instead of broken actions',async({page})=>{
 await page.route('**/api/status',route=>route.fulfill({json:{localMode:false,ai:'configured',email:'not-configured',testData:'not-configured',secret:'not-configured'}}));
 await page.goto('/');
 await expect(page.getByRole('status')).toContainText('Odesílání e-mailů zatím není nastavené');
 await page.getByLabel('Příjemce',{exact:false}).fill('test@example.com');
 await page.getByRole('button',{name:'Domluvit schůzku'}).click();
 await page.getByRole('button',{name:'Vytvořit ukázkový návrh'}).click();
 await expect(page.getByRole('button',{name:'Zkontrolovat a odeslat'})).toBeDisabled();
 await page.goto('/test-data');
 await expect(page.getByRole('status')).toContainText('Náhled testovacích dat zatím není nastavený');
 await expect(page.getByRole('button',{name:'Načíst testovací data'})).toBeDisabled();
});
test('data failure permits retry and never fabricates rows',async({page})=>{
 await page.route('**/api/test-data',route=>route.fulfill({status:502,json:{message:'Chybí oprávnění k testovací tabulce.'}}));
 await page.goto('/test-data');await page.getByRole('button',{name:'Načíst testovací data'}).click();
 await expect(page.getByRole('alert')).toContainText('Chybí oprávnění');
 await expect(page.getByRole('table')).not.toBeVisible();
 await expect(page.getByRole('button',{name:'Načíst testovací data'})).toBeEnabled();
});
test('failed access load can be retried',async({page})=>{
 let calls=0;await page.route('**/api/admin/access',route=>{calls++;return calls===1?route.fulfill({status:502,json:{message:'Služba neodpovídá.'}}):route.continue();});
 await page.goto('/admin');await expect(page.getByRole('alert')).toContainText('Služba neodpovídá');
 await page.getByRole('button',{name:'Zkusit znovu'}).click();
 await expect(page.getByRole('heading',{name:'Administrátoři'})).toBeVisible();
});

test('real HTTP accepts maximum Unicode body with JSON escaping',async({request})=>{
 const payload='{"requestId":"'+crypto.randomUUID()+'","recipient":"test@example.com","subject":"Unicode boundary","body":"'+'\\u6f22'.repeat(10000)+'"}';
 const response=await request.post('/api/send',{headers:{'Content-Type':'application/json','X-App-Request':'1'},data:payload});
 expect(response.status()).toBe(200);expect((await response.json()).simulated).toBe(true);
});

test('unapproved colleague requests only as authenticated identity and sees pending',async({page})=>{
 let applied=false;
 await page.route('**/api/me',route=>route.fulfill({status:403,json:{message:'Nemáš přístup do aplikace.'}}));
 await page.route('**/api/access-request',route=>{if(route.request().method()==='POST')applied=true;return route.fulfill({json:{email:'applicant@alza.cz',status:applied?'pending':'none',allowed:false,isAdmin:false,localMode:true}});});
 await page.setViewportSize({width:390,height:844});
 await page.goto('/');await expect(page.getByRole('heading',{name:'Požádej o přístup.'})).toBeVisible();
 await expect(page.getByText('applicant@alza.cz',{exact:true})).toBeVisible();
 await expect(page.getByLabel('Příjemce',{exact:false})).not.toBeVisible();
 await page.getByRole('button',{name:'Požádat o přístup'}).click();
 await expect(page.getByRole('heading',{name:'Žádost čeká na schválení.'})).toBeVisible();
 expect(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth)).toBe(true);
 await page.screenshot({path:'../../docs/screenshots/access-request-mobile.png',fullPage:true});
 await expect(page.getByRole('button',{name:'Požádat o přístup'})).not.toBeVisible();
 await page.getByRole('button',{name:'Zkontrolovat schválení'}).click();
 await expect(page.getByRole('heading',{name:'Žádost čeká na schválení.'})).toBeVisible();
});
test('rejected applicant stays outside application and can retry status failure',async({page})=>{
 let calls=0;
 await page.route('**/api/me',route=>route.fulfill({status:403,json:{message:'Nemáš přístup.'}}));
 await page.route('**/api/access-request',route=>{calls++;return calls===1?route.fulfill({status:502,json:{message:'Žádost se nepodařilo načíst.'}}):route.fulfill({json:{email:'rejected@alza.cz',status:'rejected',allowed:false,isAdmin:false,localMode:false}});});
 await page.goto('/');await expect(page.getByRole('alert').first()).toContainText('Žádost se nepodařilo načíst');
 await page.getByRole('button',{name:'Zkusit znovu'}).click();
 await expect(page.getByRole('heading',{name:'Žádost byla zamítnuta.'})).toBeVisible();
 await expect(page.getByRole('button',{name:'Požádat o přístup'})).not.toBeVisible();
 await expect(page.getByLabel('Příjemce',{exact:false})).not.toBeVisible();
});
test('real HTTP request approval and revocation through admin screen',async({page,request})=>{
 const email='browser-request-'+crypto.randomUUID()+'@alza.cz';
 const headers={'X-Goog-Authenticated-User-Email':'accounts.google.com:'+email,'X-App-Request':'1'};
 await page.goto('/admin');
 expect((await request.post('/api/access-request',{headers})).status()).toBe(200);
 expect((await request.get('/api/test-data',{headers})).status()).toBe(403);
 await page.getByRole('button',{name:'Obnovit žádosti'}).click();
 await page.getByRole('button',{name:'Schválit '+email}).click();
 await expect(page.getByText(email,{exact:true})).toBeVisible();
 expect((await request.get('/api/me',{headers})).status()).toBe(200);
 page.once('dialog',dialog=>dialog.accept());
 await page.getByRole('button',{name:'Odebrat '+email}).click();
 await expect(page.getByText(email,{exact:true})).not.toBeVisible();
 expect((await request.get('/api/me',{headers})).status()).toBe(403);
 const own=await request.get('/api/access-request',{headers});expect((await own.json()).status).toBe('revoked');
});
test('admin rejection removes pending request and grants no access',async({page,request})=>{
 const email='browser-reject-'+crypto.randomUUID()+'@alza.cz';
 const headers={'X-Goog-Authenticated-User-Email':'accounts.google.com:'+email,'X-App-Request':'1'};
 await page.goto('/admin');await request.post('/api/access-request',{headers});
 await page.getByRole('button',{name:'Obnovit žádosti'}).click();
 await page.getByRole('button',{name:'Zamítnout '+email}).click();
 await expect(page.getByRole('status')).toContainText('Žádost byla zamítnuta');
 expect((await request.get('/api/me',{headers})).status()).toBe(403);
 expect((await (await request.get('/api/access-request',{headers})).json()).status).toBe('rejected');
});
test('failed request refresh never claims success or keeps stale review controls',async({page})=>{
 let calls=0;
 await page.route('**/api/admin/requests',route=>{calls++;return calls===1?route.fulfill({json:[{email:'waiting@alza.cz',status:'pending',createdAt:'2026-10-08T10:00:00Z'}]}):route.fulfill({status:502,json:{message:'Žádosti se nepodařilo obnovit.'}});});
 await page.goto('/admin');await expect(page.getByRole('button',{name:'Schválit waiting@alza.cz'})).toBeVisible();
 await page.getByRole('button',{name:'Obnovit žádosti'}).click();
 await expect(page.getByRole('alert')).toContainText('Žádosti se nepodařilo obnovit');
 await expect(page.getByRole('status')).not.toBeVisible();
 await expect(page.getByRole('button',{name:'Schválit waiting@alza.cz'})).not.toBeVisible();
 await expect(page.getByRole('button',{name:'Zkusit znovu'})).toBeVisible();
});
