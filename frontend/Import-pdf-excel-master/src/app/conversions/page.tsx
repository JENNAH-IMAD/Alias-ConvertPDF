'use client';
import { useCatalog } from '@/services/use-catalog';
import { useState, type DragEvent } from 'react';
import { useRouter } from 'next/navigation';
import { Upload, FileText } from 'lucide-react';
import { Account, Client, request } from '@/services/api';
import { useAuth } from '@/components/auth-context';
import { Button } from '@/components/ui/button';
import { LoadState } from '@/components/resource-editor';
import { StatementList } from '@/components/statement-list';
export default function ConversionsPage(){
 const router=useRouter();const {can}=useAuth();const [client,setClient]=useState('');const [account,setAccount]=useState('');const [file,setFile]=useState<File|null>(null);const [busy,setBusy]=useState(false);const [error,setError]=useState('');
 const clients=useCatalog<Client>('/clients');const accounts=useCatalog<Account>('/bank-accounts');
 function choose(f?:File){setError('');if(!f)return;if(!f.name.toLowerCase().endsWith('.pdf')||f.type!=='application/pdf'||!f.size||f.size>20*1024*1024){setError('Sélectionnez un PDF non vide de 20 Mo maximum.');return;}setFile(f);}
 async function upload(){if(!file||!account||busy)return;setBusy(true);setError('');try{const form=new FormData();form.append('file',file);const result=await request<{id:string}>(`/bank-accounts/${account}/statements/upload`,{method:'POST',body:form});router.push('/statements/'+result.id);}catch(e){setError((e as Error).message);}finally{setBusy(false);}}
 function drop(e:DragEvent){e.preventDefault();if(!busy)choose(e.dataTransfer.files[0]);}
 return <div className="ui-page"><section className="ui-hero"><p className="ui-eyebrow">ASSISTANT DE CONVERSION</p><h1 className="ui-title">Nouvelle conversion</h1><p className="ui-description">Importez le relevé, contrôlez les opérations, puis générez vos exports.</p></section><div className="flex flex-wrap gap-2">{['1 · Contexte et PDF','2 · Analyse et extraction','3 · Revue et validation','4 · Export et archive'].map(s=><span className="ui-tag" key={s}>{s}</span>)}</div><LoadState error={error||clients.error||accounts.error} loading={clients.loading||accounts.loading}/>{can('statements.write')&&<section className="ui-card grid gap-6 md:grid-cols-2"><div className="space-y-4"><label className="block">Client<select className="ui-input mt-2" value={client} disabled={busy} onChange={e=>{setClient(e.target.value);setAccount('');}}><option value="">Sélectionner un client</option>{clients.data?.items.map(c=><option key={c.id} value={c.id}>{c.name}</option>)}</select></label><label className="block">Compte bancaire<select className="ui-input mt-2" value={account} disabled={busy||!client} onChange={e=>setAccount(e.target.value)}><option value="">Sélectionner un compte</option>{accounts.data?.items.filter(a=>a.clientId===client).map(a=><option key={a.id} value={a.id}>{a.bank} · {a.accountNumber} ({a.currency})</option>)}</select></label><p className="ui-description">Le PDF original est conservé. Aucun modèle bancaire ni opération ne sera inventé si le format n’est pas reconnu.</p></div><div className="ui-upload" onDragOver={e=>e.preventDefault()} onDrop={drop}><Upload size={30}/><label className="mt-4 text-center">Déposer un relevé PDF<input aria-label="Choisir le relevé PDF" className="ui-file" type="file" accept="application/pdf,.pdf" disabled={busy} onChange={e=>choose(e.target.files?.[0])}/></label>{file&&<p className="ui-description break-all"><FileText size={15} className="inline"/> {file.name} · {(file.size/1024).toFixed(0)} Ko</p>}<Button className="button-primary mt-5" disabled={busy||!account||!file} onClick={upload}>{busy?'Import en cours…':'Importer et continuer'}</Button></div></section>}<StatementList/></div>;
}

