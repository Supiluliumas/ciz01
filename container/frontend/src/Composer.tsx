import {useEffect,useRef,useState} from 'react';
import {ArrowRight,Check,CheckCircle2,Copy,Mail,Plus,Send,Sparkles,LoaderCircle,X,CalendarDays,MessageSquare,HeartHandshake,TriangleAlert} from 'lucide-react';
import {api,ApiError,errorMessage,type Status} from './api';
type Draft={subject:string,body:string,simulated:boolean};
export function Composer({localMode}:{localMode:boolean}){
 const [recipient,setRecipient]=useState('');const [instructions,setInstructions]=useState('');const [tone,setTone]=useState('professional');const [language,setLanguage]=useState('cs');
 const [draft,setDraft]=useState<Draft>();const [generating,setGenerating]=useState(false);const [sending,setSending]=useState(false);const [review,setReview]=useState(false);const [error,setError]=useState('');const [outcome,setOutcome]=useState('');const [copied,setCopied]=useState(false);const [uncertain,setUncertain]=useState(false);const [requestId,setRequestId]=useState(()=>crypto.randomUUID());
 const [mailOff,setMailOff]=useState(false);
 const reviewButton=useRef<HTMLButtonElement>(null);const dialog=useRef<HTMLDialogElement>(null);
 useEffect(()=>{if(review)dialog.current?.showModal();else dialog.current?.close();},[review]);
 useEffect(()=>{api<Status>('/status').then(s=>setMailOff(s.email==='not-configured')).catch(()=>{});},[]);
 function reset(){setRecipient('');setInstructions('');setDraft(undefined);setError('');setOutcome('');setUncertain(false);setRequestId(crypto.randomUUID());}
 async function generate(event:React.FormEvent){event.preventDefault();setGenerating(true);setError('');setOutcome('');try{const result=await api<Draft>('/drafts','POST',{recipient,instructions,tone,language});setDraft(result);setUncertain(false);setRequestId(crypto.randomUUID());}catch(e){setError(errorMessage(e));}finally{setGenerating(false);}}
 function edit(field:'subject'|'body',value:string){if(draft)setDraft({...draft,[field]:value});setOutcome('');setRequestId(crypto.randomUUID());}
 async function send(){if(!draft||sending)return;setSending(true);setError('');try{const result=await api<{status:string,simulated:boolean}>('/send','POST',{requestId,recipient,subject:draft.subject,body:draft.body});setOutcome(result.simulated?'Ukázkové odeslání dokončeno. Žádný skutečný e-mail nebyl odeslán.':'n8n potvrdilo přijetí e-mailu k odeslání. Doručení ověř ve schránce příjemce.');setReview(false);}catch(e){setError(errorMessage(e));if(!(e instanceof ApiError)||[409,502,504].includes(e.status))setUncertain(true);setReview(false);}finally{setSending(false);}}
 async function copy(){if(!draft)return;try{await navigator.clipboard.writeText(`${draft.subject}\n\n${draft.body}`);setCopied(true);setTimeout(()=>setCopied(false),2000);}catch{setError('Kopírování není dostupné. Označ text a zkopíruj ho ručně.');}}
 return <>
   <div className="page-heading"><div><span className="eyebrow">MALÁ POMOC. LEPŠÍ KOMUNIKACE.</span><h1>Napiš méně. <span>Řekni víc.</span></h1><p>Proměň pár myšlenek v e-mail, který můžeš rovnou použít.</p></div><button className="secondary" onClick={reset} disabled={generating||sending}><Plus size={16}/>Nová zpráva</button></div>
   <div className="steps"><span className="current"><b>1</b>Zadej myšlenku</span><ArrowRight size={14}/><span className={draft?'current':''}><b>2</b>Uprav návrh</span><ArrowRight size={14}/><span><b>3</b>Zkontroluj a odešli</span></div>
   {mailOff&&<div className="notice info" role="status">Odesílání e-mailů zatím není nastavené — návrh můžeš vytvořit a zkopírovat, odeslání se zapne po připojení služby.</div>}
   {error&&<div className="notice error" role="alert"><TriangleAlert size={18}/>{error}<button onClick={()=>setError('')} aria-label="Zavřít chybu"><X size={16}/></button></div>}
   {uncertain&&<div className="notice info" role="status">Výsledek odeslání je nejistý. Tento pokus znovu neodesíláme. Nejdřív ověř běh v n8n, než vytvoříš novou zprávu.</div>}
   {outcome&&<div className="notice success" role="status"><CheckCircle2 size={18}/>{outcome}</div>}
   <div className="composer-grid">
     <section className="panel input-panel"><div className="panel-title"><span className="number">01</span><div><h2>Co chceš napsat?</h2><p>Stačí stručně. O zbytek se postará návrh.</p></div></div>
       <form onSubmit={generate}>
         <label htmlFor="recipient">Příjemce <span>*</span></label><div className="input-with-icon"><Mail size={17}/><input id="recipient" type="email" required maxLength={254} placeholder="kolega@example.com" value={recipient} disabled={sending} onChange={e=>{setRecipient(e.target.value);setRequestId(crypto.randomUUID());setOutcome('');}}/></div>
         <div className="label-row"><label htmlFor="instructions">Tvoje zadání <span>*</span></label><span>{instructions.length} / 4 000</span></div>
         <textarea id="instructions" required minLength={10} maxLength={4000} rows={6} placeholder="Například: Napiš kolegovi, že bych rád domluvil schůzku k novému projektu. Zeptej se, kdy má příští týden čas." value={instructions} onChange={e=>setInstructions(e.target.value)} disabled={generating||sending}/>
         <span className="field-hint">Přidej účel, důležité body a případně oslovení.</span>
         <fieldset disabled={generating||sending}><legend>Tón zprávy</legend><div className="tone-options">{[['professional','Profesionální'],['friendly','Přátelský'],['concise','Stručný']].map(([value,label])=><label key={value} className={tone===value?'tone selected':'tone'}><input type="radio" name="tone" value={value} checked={tone===value} onChange={()=>setTone(value)}/>{label}</label>)}</div></fieldset>
         <label htmlFor="language">Jazyk e-mailu</label><select id="language" disabled={generating||sending} value={language} onChange={e=>setLanguage(e.target.value)}><option value="cs">Čeština</option><option value="en">English</option></select>
         <button className="primary generate-button" type="submit" disabled={generating||sending}>{generating?<LoaderCircle className="spin" size={18}/>:<Sparkles size={18}/>} {generating?'Připravuju návrh…':localMode?'Vytvořit ukázkový návrh':'Vytvořit návrh pomocí AI'}<ArrowRight size={17}/></button>
         <p className="tiny centered">Návrh můžeš před odesláním libovolně upravit.</p>
       </form>
     </section>
     <section className="panel draft-panel"><div className="panel-title"><span className="number">02</span><div><h2>Tvůj návrh e-mailu</h2><p>{draft?'Dolaď detaily. Poslední slovo máš ty.':'Tady se tvoje myšlenka promění ve zprávu.'}</p></div>{draft&&<button className="icon-button" onClick={copy} aria-label="Zkopírovat návrh">{copied?<Check size={18}/>:<Copy size={18}/>}</button>}</div>
       {!draft?<div className="empty-draft"><div className="letter-illustration"><div className="paper-lines"><span/><span/><span/></div><Mail size={56} strokeWidth={1.2}/><span className="sparkle-bubble"><Sparkles size={17}/></span></div><h3>Dobré zprávy začínají myšlenkou.</h3><p>Vyplň zadání vlevo a vytvoř první návrh.<br/>Odeslání je vždy na tobě.</p><span className="draft-tag"><Sparkles size={12}/>Připraveno pro tvoje nápady</span></div>:<div className="draft-editor">
          {draft.simulated&&<span className="sample-tag">Ukázkový návrh · bez volání AI</span>}
          <label htmlFor="subject">Předmět</label><input id="subject" value={draft.subject} maxLength={200} onChange={e=>edit('subject',e.target.value)} disabled={sending}/>
          <div className="draft-to">Komu: <strong>{recipient}</strong></div><label htmlFor="body">Text e-mailu</label><textarea id="body" value={draft.body} maxLength={10000} rows={12} onChange={e=>edit('body',e.target.value)} disabled={sending}/>
          <div className="draft-actions"><span><CheckCircle2 size={14}/>Před odesláním zkontroluj obsah.</span><button ref={reviewButton} className="primary" onClick={()=>setReview(true)} disabled={!draft.subject.trim()||!draft.body.trim()||!recipient||!!outcome||uncertain||generating||sending||mailOff}><Send size={16}/>Zkontrolovat a odeslat</button></div>
       </div>}
     </section>
   </div>
   <section className="inspiration"><div><h3>Chybí ti první věta?</h3><p>Zkus některý z běžných scénářů.</p></div>{[[CalendarDays,'Domluvit schůzku','Napiš kolegovi a požádej ho o vhodný termín schůzky k novému projektu příští týden.'],[MessageSquare,'Připomenout odpověď','Napiš zdvořilou připomínku a požádej kolegu o odpověď na předchozí zprávu.'],[HeartHandshake,'Poděkovat kolegovi','Napiš krátké poděkování kolegovi za pomoc s dokončením společného projektu.']].map(([Icon,label,prompt])=>{const I=Icon as typeof CalendarDays;return <button key={label as string} onClick={()=>setInstructions(prompt as string)} disabled={generating||sending}><I size={17}/>{label as string}<ArrowUpRightIcon/></button>;})}</section>
   <dialog ref={dialog} className="review-dialog" aria-label="Kontrola e-mailu před odesláním" onCancel={event=>{if(sending)event.preventDefault();else setReview(false);}} onClose={()=>{if(!sending)setReview(false);}}>
       <div className="modal-header"><span className="modal-icon"><Send size={23}/></span><button className="icon-button" disabled={sending} onClick={()=>{setReview(false);reviewButton.current?.focus();}} aria-label="Zavřít kontrolu"><X size={20}/></button></div>
       <h2>Je všechno připravené?</h2><p>Zkontroluj adresu příjemce a finální text zprávy.</p>
       <div className="review-recipient"><span>PŘÍJEMCE</span><strong>{recipient}</strong></div><div className="review-message"><strong>{draft?.subject}</strong><p>{draft?.body}</p></div>
       {localMode&&<div className="notice info">Toto je ukázka. Skutečný e-mail se neodešle.</div>}
       <div className="modal-actions"><button className="secondary" disabled={sending} onClick={()=>setReview(false)}>Zpět k úpravám</button><button className="primary" disabled={sending} onClick={send}>{sending?<LoaderCircle className="spin" size={16}/>:<Send size={16}/>} {sending?'Odesílám…':localMode?'Potvrdit ukázkové odeslání':'Potvrdit a odeslat'}</button></div>
   </dialog>
 </>;
}
function ArrowUpRightIcon(){return <svg width="13" height="13" viewBox="0 0 16 16" fill="none" aria-hidden="true"><path d="M4 12 12 4M4 4h8v8" stroke="currentColor" strokeWidth="1.5"/></svg>;}
