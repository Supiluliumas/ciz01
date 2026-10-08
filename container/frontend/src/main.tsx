import React,{useEffect,useState} from 'react';
import {createRoot} from 'react-dom/client';
import {ArrowUpRight,Mail,PenLine,Database,ShieldCheck,Leaf,ChevronRight,FlaskConical,LoaderCircle,LockKeyhole} from 'lucide-react';
import {api,ApiError,errorMessage,type Me} from './api';
import {Composer} from './Composer';
import {Admin} from './Admin';
import {AccessRequest} from './AccessRequest';
import {TestData} from './TestData';
import './styles.css';

function App(){
 const [me,setMe]=useState<Me>();const [error,setError]=useState('');const [blocked,setBlocked]=useState(false);
 useEffect(()=>{api<Me>('/me').then(setMe).catch(e=>{setError(errorMessage(e));setBlocked(e instanceof ApiError&&e.status===403);});},[]);
 const path=window.location.pathname;
 const title=path==='/admin'?'Správa přístupu':path==='/test-data'?'Testovací data':'Nový e-mail';
 if(blocked)return <AccessRequest denial={error}/>;
 if(error)return <main className="access-page"><LockKeyhole size={38}/><h1>Aplikace není dostupná</h1><p role="alert">{error}</p><button className="primary" onClick={()=>window.location.reload()}>Zkusit znovu</button></main>;
 if(!me)return <main className="access-page"><LoaderCircle className="spin"/><p>Ověřuju přístup…</p></main>;
 return <div className="app-shell">
   <aside className="sidebar">
     <a href="/" className="brand"><span className="brand-mark"><Mail size={22}/></span><span>AI Mailer<span className="brand-sub">AI ACADEMY</span></span></a>
     <div className="nav-caption">TVŮJ PRACOVNÍ PROSTOR</div>
     <nav aria-label="Hlavní navigace">
       <a className={path==='/'?'nav-item active':'nav-item'} href="/"><PenLine size={19}/>Nový e-mail<ChevronRight size={15}/></a>
       <a className={path==='/test-data'?'nav-item active':'nav-item'} href="/test-data"><Database size={19}/>Testovací data</a>
       {me.isAdmin&&<a className={path==='/admin'?'nav-item active':'nav-item'} href="/admin"><ShieldCheck size={19}/>Správa přístupu</a>}
     </nav>
     <div className="sidebar-note"><Leaf size={21}/><h3>Od nápadu ke zprávě.</h3><p>Ty víš, co chceš říct.<br/>S formulací ti pomůže AI.</p><span>Vytvořeno v AI Academy <ArrowUpRight size={13}/></span></div>
     <div className="profile"><span className="avatar">{me.email.slice(0,2).toUpperCase()}</span><div><strong>{me.localMode?'Ukázkový uživatel':me.email.split('@')[0]}</strong><span>{me.isAdmin?'Administrátor':'Uživatel'}</span></div><span className="online-dot"/></div>
   </aside>
   <div className="main-shell">
     <header className="topbar"><div><span className="breadcrumb">Pracovní prostor</span><ChevronRight size={14}/><span>{title}</span></div><span className="project-badge"><span className="online-dot"/>AI Academy · testovací projekt</span></header>
     {me.localMode&&<div className="demo-banner"><FlaskConical size={17}/><span><strong>Lokální ukázka.</strong> AI odpovědi a odesílání jsou simulované. Žádný e-mail se neodesílá; nastavení přístupu se při restartu smaže.</span></div>}
     <main className="content">
       {path==='/admin'?(me.isAdmin?<Admin me={me}/>:<section className="panel"><h1>Nemáš oprávnění správce.</h1></section>):path==='/test-data'?<TestData/>:<Composer localMode={me.localMode}/>}
       <footer><span>ciz01</span><span><ShieldCheck size={13}/> Obsah e-mailů se v aplikaci neukládá.</span></footer>
     </main>
   </div>
 </div>;
}
createRoot(document.getElementById('root')!).render(<React.StrictMode><App/></React.StrictMode>);
