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
