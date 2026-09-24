'use client';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';

import { Skeleton } from './ui/skeleton';
import { FormEvent, useEffect, useState } from 'react';
import { Dialog as BankDialog } from './dialog';
import { ChevronLeft, ChevronRight, Inbox } from 'lucide-react';
import { all, Bank, Client, request } from '@/services/api';

type Field = { key: string; label: string; type?: 'number' | 'email' | 'checkbox' | 'bank' | 'client' | 'select'; options?: [string, string][]; optional?: boolean };
type Schema = { defaults: Record<string, unknown>; fields: Field[]; };
const schemas: Record<string, Schema> = {
  clients: { defaults: { country: 'Maroc' }, fields: [{key:'name',label:'Nom'}, {key:'legalName',label:'Raison sociale'}, {key:'ice',label:'ICE (15 chiffres)'}, {key:'if',label:'IF'}, {key:'rc',label:'RC'}, {key:'email',label:'Email',type:'email'}, {key:'phone',label:'Téléphone'}, {key:'country',label:'Pays'}] },
  banks: { defaults: {}, fields: [{key:'name',label:'Nom'}, {key:'code',label:'Code'}, {key:'description',label:'Description',optional:true}] },
  'bank-accounts': { defaults: {currency:'MAD',journal:'BQ',accountCode:'512000'}, fields: [{key:'clientId',label:'Client',type:'client'}, {key:'bankId',label:'Banque',type:'bank'}, {key:'accountNumber',label:'Numéro de compte'}, {key:'accountName',label:'Intitulé'}, {key:'currency',label:'Devise'}, {key:'journal',label:'Journal comptable'}, {key:'accountCode',label:'Compte comptable'}] },
};
export function ResourceEditor({ resource, initial, onClose, onSaved }: {resource:string; initial?: object; onClose:()=>void; onSaved:()=>void}) {
  const schema = schemas[resource];
  const [values,setValues] = useState<Record<string, unknown>>({...schema.defaults,...initial});
  const [banks,setBanks] = useState<Bank[]>([]); const [clients,setClients] = useState<Client[]>([]);
  const [error,setError] = useState(''); const [busy,setBusy] = useState(false);
  useEffect(()=> { if(schema.fields.some(f=>f.type==='bank')) all<Bank>('/banks').then(setBanks).catch(e=>setError(e.message)); if(schema.fields.some(f=>f.type==='client')) all<Client>('/clients').then(setClients).catch(e=>setError(e.message)); },[schema]);
  const inputClass = "ui-input";
  const set = (key:string, value:unknown) => setValues(v=>({...v,[key]:value}));
  const submit = async (e:FormEvent) => {
    e.preventDefault(); if(busy)return; setBusy(true); setError('');
    const payload = {...values};
    for (const f of schema.fields) { if (payload[f.key] === undefined) payload[f.key] = f.type === 'number' ? null : ''; }
    try { await request(`/${resource}${values.id ? `/${values.id}`:''}`, {method:values.id?'PUT':'POST',body:JSON.stringify(payload)}); onSaved(); onClose(); } catch(e) {setError((e as Error).message);} finally {setBusy(false);}
  };
  return <BankDialog title={(values.id?'Modifier':'Créer')+' · '+({'clients':'Client','banks':'Banque','bank-accounts':'Compte bancaire'}[resource]||'Référentiel')} onClose={onClose} busy={busy}><form onSubmit={submit} className="space-y-5"><fieldset disabled={busy} className="space-y-5">
    <div className="grid gap-4 sm:grid-cols-2">{schema.fields.map(f=><label key={f.key} className="space-y-2 text-sm"><span>{f.label}</span>{f.type==='checkbox'?<input type="checkbox" className="ml-3" checked={Boolean(values[f.key])} onChange={e=>set(f.key,e.target.checked)}/>:['bank','client','select'].includes(f.type||'')?<select required className={inputClass} value={String(values[f.key]??'')} onChange={e=>set(f.key,e.target.value)}><option value="">Choisir…</option>{(f.type==='bank'?banks.map(b=>[b.id,b.name]):f.type==='client'?clients.map(c=>[c.id,c.name]):f.options||[]).map(([value,label])=><option key={value} value={value}>{label}</option>)}</select>:<Input className={inputClass} type={f.type||'text'} required={!f.optional} min={f.type==='number'?0:undefined} value={String(values[f.key]??'')} onChange={e=>set(f.key,f.type==='number'?(e.target.value===''?null:Number(e.target.value)):e.target.value)}/>}</label>)}</div>
    </fieldset>{error&&<p role="alert" className="text-rose-500">{error}</p>}<Button variant="ghost" disabled={busy} className="button-primary">{busy?'Enregistrement…':'Enregistrer'}</Button>
  </form></BankDialog>;
}
export function LoadState({error,loading,empty}:{error:string;loading:boolean;empty?:boolean}) { return <>{error&&<p role="alert" className="rounded-xl bg-rose-500/10 p-4 text-rose-500">{error}</p>}{loading&&<div className="load-state" role="status"><span className="sr-only">Chargement en cours</span><div className="loading-skeleton" aria-hidden="true"><Skeleton className="h-10 w-10 rounded-xl"/><div className="flex-1 space-y-2"><Skeleton className="h-3 w-1/3"/><Skeleton className="h-3 w-2/3"/></div></div></div>}{!loading&&!error&&empty&&<div className="load-state flex-col"><Inbox size={28}/><p>Aucun élément à afficher.</p><p className="text-xs">Ajoutez un élément ou ajustez vos filtres pour commencer.</p></div>}</>; }
export function Pagination({page,total,onChange}:{page:number;total:number;onChange:(n:number)=>void}) { return <nav aria-label="Pagination" className="pagination"><span>{total} élément{total>1?'s':''} · Page {page} sur {Math.max(1,Math.ceil(total/20))}</span><div className="flex gap-2"><Button variant="ghost" className="button-secondary" disabled={page===1} onClick={()=>onChange(page-1)}><ChevronLeft size={14}/> Précédent</Button><Button variant="ghost" className="button-secondary" disabled={page*20>=total} onClick={()=>onChange(page+1)}>Suivant <ChevronRight size={14}/></Button></div></nav>; }
