import {useEffect,useState} from 'react';
import {Database,Download,FlaskConical,LoaderCircle,TriangleAlert} from 'lucide-react';
import {api,errorMessage,type Status} from './api';
type Preview={columns:string[],rows:string[][],simulated:boolean,source:string};
export function TestData(){
 const [preview,setPreview]=useState<Preview>();const [busy,setBusy]=useState(false);const [error,setError]=useState('');
 const [notConfigured,setNotConfigured]=useState(false);
 useEffect(()=>{api<Status>('/status').then(s=>setNotConfigured(s.testData==='not-configured')).catch(()=>{});},[]);
 async function load(){setBusy(true);setError('');try{setPreview(await api<Preview>('/test-data'));}catch(e){setError(errorMessage(e));}finally{setBusy(false);}}
 return <>
   <div className="page-heading"><div><span className="eyebrow">UKÁZKA PRO AI ACADEMY</span><h1>Testovací <span>data.</span></h1><p>Malý náhled připraveného datasetu. Jen ke čtení, bez zásahu do dat.</p></div><button className="primary" disabled={busy||notConfigured} onClick={load}>{busy?<LoaderCircle className="spin" size={16}/>:<Download size={16}/>} {busy?'Načítám…':preview?'Obnovit náhled':'Načíst testovací data'}</button></div>
   {notConfigured&&<div className="notice info" role="status">Náhled testovacích dat zatím není nastavený — zapne se po doplnění připojení na BigQuery.</div>}
   {error&&<div className="notice error" role="alert"><TriangleAlert size={18}/>{error}</div>}
   <section className="panel data-panel"><div className="panel-title"><span className="number"><Database size={18}/></span><div><h2>Náhled datasetu</h2><p>Nejvýše 20 řádků. Data se nevkládají do e-mailů.</p></div>{preview&&<span className="sample-tag">{preview.rows.length} řádků</span>}</div>
   {!preview?<div className="empty-state"><span className="data-icon"><Database size={38}/></span><h3>Data načteme, až budeš chtít.</h3><p>Klikni na „Načíst testovací data“.<br/>Aplikace nemění žádné záznamy.</p></div>:<>
   <div className="data-source">{preview.simulated?<FlaskConical size={15}/>:<Database size={15}/>}<span>{preview.simulated?'Ukázková data · BigQuery se nevolá':preview.source}</span></div>
   {preview.rows.length===0?<div className="empty-state"><p>Testovací tabulka je prázdná.</p></div>:<div className="table-scroll"><table><thead><tr>{preview.columns.map(column=><th key={column}>{column}</th>)}</tr></thead><tbody>{preview.rows.map((row,index)=><tr key={index}>{row.map((cell,i)=><td key={i}>{cell||'—'}</td>)}</tr>)}</tbody></table></div>}
   </>}</section><div className="data-note"><ShieldIcon/><div><strong>Oddělené od tvých zpráv.</strong><p>Testovací dataset slouží pouze k demonstraci čtení dat v zadání akademie.</p></div></div>
 </>;
}
function ShieldIcon(){return <FlaskConical size={23}/>;}
