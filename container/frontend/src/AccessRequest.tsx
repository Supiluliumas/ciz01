import {useEffect,useState} from 'react';
import {LockKeyhole,LoaderCircle,ShieldCheck,RefreshCw} from 'lucide-react';
import {api,errorMessage} from './api';
type RequestStatus={email:string,status:'none'|'pending'|'approved'|'rejected'|'revoked',allowed:boolean,isAdmin:boolean,localMode:boolean};
export function AccessRequest({denial}:{denial:string}){
 const [request,setRequest]=useState<RequestStatus>();const [busy,setBusy]=useState(false);const [error,setError]=useState('');
 async function load(){setBusy(true);setError('');try{setRequest(await api<RequestStatus>('/access-request'));}catch(e){setError(errorMessage(e));}finally{setBusy(false);}}
 useEffect(()=>{void load();},[]);
 async function submit(){setBusy(true);setError('');try{setRequest(await api<RequestStatus>('/access-request','POST'));}catch(e){setError(errorMessage(e));}finally{setBusy(false);}}
 const pending=request?.status==='pending';const rejected=request?.status==='rejected';
 return <main className="access-page request-page">
  <LockKeyhole size={38}/><span className="eyebrow">AI MAILER · PŘÍSTUP NA SCHVÁLENÍ</span>
  <h1>{request?.allowed?'Přístup je schválený.':pending?'Žádost čeká na schválení.':rejected?'Žádost byla zamítnuta.':'Požádej o přístup.'}</h1>
  <p>{request?.email&&<>Přihlášený účet: <strong>{request.email}</strong><br/></>}{request?.allowed?'Můžeš pokračovat do aplikace.':pending?'Správce musí nejdřív schválit tvou konkrétní adresu.':rejected?'Přístup nemáš povolený. Pokud potřebuješ rozhodnutí změnit, obrať se na správce.':request?.status==='revoked'?'Tvůj dřívější přístup byl odebrán. Můžeš podat novou žádost.':'Firemní přihlášení ověřilo tvou identitu. Používání aplikace musí schválit správce.'}</p>
  {request?.localMode&&<p className="tiny">Lokální ukázka · žádosti jsou v paměti a při restartu se smažou.</p>}
  {error&&<p role="alert">{error}</p>}
  {!request&&<p role="alert">{denial}</p>}
  {!request&&!busy&&<button className="secondary" onClick={()=>void load()}>Zkusit znovu</button>}
  {busy&&<LoaderCircle className="spin" aria-label="Načítám žádost"/>}
  {request&&!request.allowed&&!pending&&!rejected&&<button className="primary" disabled={busy} onClick={()=>void submit()}>Požádat o přístup</button>}
  {request&&!request.allowed&&<button className="secondary" disabled={busy} onClick={()=>void load()}><RefreshCw size={16}/>Zkontrolovat schválení</button>}
  {request?.allowed&&<button className="primary" onClick={()=>window.location.reload()}><ShieldCheck size={16}/>Otevřít aplikaci</button>}
  <p className="tiny">Bez schválení nemůžeš používat AI, odesílat e-maily ani číst testovací data.</p>
 </main>;
}
