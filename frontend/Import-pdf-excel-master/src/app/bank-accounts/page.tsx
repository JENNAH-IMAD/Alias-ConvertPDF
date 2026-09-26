'use client';
import { useState } from 'react';

import { ResourceEditor, LoadState, Pagination } from '@/components/resource-editor';
import { Account, Page, request } from '@/services/api';
import { useApi } from '@/services/use-api';
export default function BankAccountsPage() {
  const [page,setPage]=useState(1);
  const {data,error,loading,reload}=useApi<Page<Account>>('/bank-accounts?page='+page+'&pageSize=20');
  const [editing,setEditing]=useState<Account|null|undefined>(undefined); const [actionError,setActionError]=useState('');
  const remove=async(item:Account)=>{if(!confirm('Supprimer ce compte bancaire ?'))return;try{await request('/bank-accounts/'+item.id,{method:'DELETE'});reload();}catch(e){setActionError((e as Error).message);}};
  const card="ui-card";
  return <div className="ui-page"><section className="ui-hero"><p className="ui-eyebrow mb-2">RÉFÉRENTIEL</p><div className="flex flex-wrap items-center justify-between gap-4"><h1 className="ui-title">Comptes bancaires</h1><button className="button-primary" onClick={()=>setEditing(null)}>Nouveau compte</button></div><p className="ui-description">Associez chaque compte à un client et à une banque, puis configurez le journal et le compte comptable.</p></section>
    <LoadState error={error||actionError} loading={loading} empty={!data?.items.length}/>
    <section className="grid gap-5 md:grid-cols-2">{data?.items.map(a=><article className={card} key={a.id}><h2 className="text-lg font-semibold">{a.accountName}</h2><p>{a.bank} · {a.accountNumber}</p><p className="my-3">{a.currency} · Journal {a.journal} · Compte {a.accountCode}</p><div className="flex gap-4"><button className="button-secondary" onClick={()=>setEditing(a)}>Modifier</button><button className="button-secondary danger-button" onClick={()=>remove(a)}>Supprimer</button></div></article>)}</section>
    <Pagination page={page} total={data?.total||0} onChange={setPage}/>{editing!==undefined&&<ResourceEditor resource="bank-accounts" initial={editing||undefined} onClose={()=>setEditing(undefined)} onSaved={reload}/>}</div>;
}
