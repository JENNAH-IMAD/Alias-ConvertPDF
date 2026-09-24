'use client';
import { useCatalog } from '@/services/use-catalog';
import Link from 'next/link';
import { useState } from 'react';
import { Archive, FileText, ArrowUpRight } from 'lucide-react';
import { Page, Client, Account } from '@/services/api';
import { Statement,statusLabel } from '@/services/statements';
import { useApi } from '@/services/use-api';
import { LoadState, Pagination } from '@/components/resource-editor';
import { AnimatedCard as Card } from '@/components/ui/card';

export function StatementList({archived=false,clientId='',accountId=''}:{archived?:boolean;clientId?:string;accountId?:string}) {
 const [page,setPage]=useState(1);const [status,setStatus]=useState('');
 const {data,error,loading}=useApi<Page<Statement>>(`/statements?page=${page}&archived=${archived}&status=${status}${clientId?'&clientId='+clientId:''}${accountId?'&accountId='+accountId:''}`);
 return <div className="ui-page"><div className="flex flex-wrap items-center justify-between gap-3"><h2 className="text-lg font-semibold">{archived?'Archives des relevés':'Relevés bancaires'}</h2><label className="text-sm">Statut<select className="ui-input mt-2" value={status} onChange={e=>{setStatus(e.target.value);setPage(1);}}><option value="">Tous les statuts</option>{Object.entries(statusLabel).map(([v,l])=><option key={v} value={v}>{l}</option>)}</select></label></div><LoadState error={error} loading={loading} empty={!data?.items.length}/><div className="grid gap-4 lg:grid-cols-2">{data?.items.map(s=><Card key={s.id} className="ui-card"><div className="flex items-start gap-3"><FileText className="shrink-0 muted" size={22}/><div className="min-w-0 flex-1"><h3 className="font-semibold break-all">{s.originalFileName}</h3><p className="ui-description">{s.client} · {s.bank}</p></div><span className="ui-tag">{statusLabel[s.status]||s.status}</span></div><div className="ui-row"><span>Compte</span><span className="break-all">{s.account}</span></div><div className="ui-row"><span>Période</span><span>{s.periodStart||'À renseigner'} — {s.periodEnd||'—'}</span></div><div className="ui-row"><span>Opérations</span><span>{s.transactionCount} · {s.currency}</span></div><div className="mt-4 flex justify-between items-center gap-3"><span className="muted text-xs">{new Date(s.createdAt).toLocaleDateString('fr-FR')}</span><Link className="button-secondary" href={'/statements/'+s.id}>Ouvrir le relevé<ArrowUpRight size={15}/></Link></div></Card>)}</div>{data&&<Pagination page={page} total={data.total} onChange={setPage}/>}</div>;
}
export function ArchivePage() {
 const [client,setClient]=useState('');const [account,setAccount]=useState('');
 const {data:clients}=useCatalog<Client>('/clients');const {data:accounts}=useCatalog<Account>('/bank-accounts');
 return <div className="ui-page"><section className="ui-hero"><p className="ui-eyebrow">CONSERVATION ET TRAÇABILITÉ</p><h1 className="ui-title">Archives clients</h1><p className="ui-description">Retrouvez les PDF originaux, les opérations, les exports et leur historique sans retraitement.</p></section><div className="ui-card flex flex-wrap gap-4"><Archive size={23}/><label className="flex-1">Client<select className="ui-input mt-2" value={client} onChange={e=>{setClient(e.target.value);setAccount('');}}><option value="">Tous les clients</option>{clients?.items.map(c=><option key={c.id} value={c.id}>{c.name}</option>)}</select></label><label className="flex-1">Compte<select className="ui-input mt-2" value={account} onChange={e=>setAccount(e.target.value)}><option value="">Tous les comptes</option>{accounts?.items.filter(a=>!client||a.clientId===client).map(a=><option key={a.id} value={a.id}>{a.bank} · {a.accountNumber}</option>)}</select></label></div><StatementList key={client+account} archived clientId={client} accountId={account}/></div>;
}

