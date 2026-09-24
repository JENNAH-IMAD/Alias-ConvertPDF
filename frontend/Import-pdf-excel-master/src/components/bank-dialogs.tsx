'use client';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Button } from '@/components/ui/button';

import { FormEvent, useState } from 'react';
import Link from 'next/link';
import { useAuth } from './auth-context';
import { Dialog as BankDialog } from './dialog';
import { CatalogDelete } from './catalog-delete';
import { Bank, Page, request } from '@/services/api';
import { useApi } from '@/services/use-api';


export function BankEditor({ initial, onClose, onSaved }: { initial: Bank | null; onClose: () => void; onSaved: (bank: Bank) => void }) {

  const [name, setName] = useState(initial?.name || ''); const [code, setCode] = useState(initial?.code || ''); const [description, setDescription] = useState(initial?.description || '');
  const [busy, setBusy] = useState(false); const [error, setError] = useState('');
  const input = "ui-input mt-2";
  async function save(e: FormEvent) {
    e.preventDefault(); if (busy) return; setBusy(true); setError('');
    try { const saved = await request<Bank>('/banks' + (initial ? '/' + initial.id : ''), { method: initial ? 'PUT' : 'POST', body: JSON.stringify({ name: name.trim(), code: code.trim(), description: description.trim() }) }); onSaved(saved); onClose(); }
    catch (e) { setError((e as Error).message); setBusy(false); }
  }
  return <BankDialog title={initial ? 'Modifier la banque' : 'Ajouter une banque'} onClose={onClose} busy={busy}>
    <form onSubmit={save} className="space-y-5"><fieldset disabled={busy} className="space-y-5">
      <label className="block text-sm">Nom de la banque *<Input autoFocus required maxLength={200} className={input} value={name} onChange={e => setName(e.target.value)} placeholder="Ex. Banque Populaire" /></label>
      <label className="block text-sm">Code unique *<Input required maxLength={30} className={input} value={code} onChange={e => setCode(e.target.value.toUpperCase())} placeholder="Ex. BP" /><span className="mt-2 block opacity-70">Lettres, chiffres, tirets et underscores. Ce code identifie la banque.</span></label>
      <label className="block text-sm">Description (facultatif)<Textarea rows={4} maxLength={2000} className={input} value={description} onChange={e => setDescription(e.target.value)} placeholder="Notes sur cette banque" /></label>
      <p className="text-sm opacity-70">Les comptes bancaires sont configurés séparément.</p>
    </fieldset>{error && <p role="alert" className="rounded-xl bg-rose-500/10 p-3 text-rose-500">{error}</p>}
      <div className="flex justify-end gap-3"><Button variant="ghost" type="button" disabled={busy} onClick={onClose} className="rounded-xl border border-current/20 px-4 py-2.5">Annuler</Button><Button variant="ghost" disabled={busy} className="button-primary">{busy ? 'Enregistrement…' : 'Enregistrer'}</Button></div>
    </form>
  </BankDialog>;
}
export function BankDelete({bank,onClose,onDeleted}:{bank:Bank;onClose:()=>void;onDeleted:()=>void}){return <CatalogDelete resource="banks" id={bank.id} name={bank.name} onClose={onClose} onDeleted={onDeleted}/>;}
type Related = { id: string; client: string; accountName: string; accountNumber: string; currency: string };
function RelatedList({ bankId }: { bankId: string }) {
  const [page, setPage] = useState(1);
  const { data, error, loading, reload } = useApi<Page<Related>>(`/banks/${bankId}/accounts?page=${page}&pageSize=20`);
  return <div className="mt-4 space-y-3">
    {loading && <p role="status">Chargement…</p>}
    {error && <div role="alert">{error} <Button variant="ghost" onClick={reload}>Réessayer</Button></div>}
    {!loading && !error && data?.items.length === 0 && <p>Aucun compte associé.</p>}
    {data?.items.map(item => <div className="rounded-xl border border-current/15 p-3 text-sm" key={item.id}><p className="font-semibold">{item.accountName} · {item.client}</p><p className="mt-1 opacity-70">{item.accountNumber} · {item.currency}</p></div>)}
    {(data?.total || 0) > 20 && <div className="flex justify-between"><Button variant="ghost" disabled={page === 1 || loading} onClick={() => setPage(p => p - 1)}>Précédent</Button><span>Page {page}</span><Button variant="ghost" disabled={page * 20 >= (data?.total || 0) || loading} onClick={() => setPage(p => p + 1)}>Suivant</Button></div>}
  </div>;
}
export function BankDetails({ bank, onClose }: { bank: Bank; onClose: () => void }) {
  const {can}=useAuth();
  return <BankDialog title={bank.name} onClose={onClose}>
    <p className="mb-3 text-sm opacity-70">Code : {bank.code}</p><p className="whitespace-pre-wrap break-words">{bank.description || 'Aucune description.'}</p>
    <h3 className="mt-6 font-semibold">Comptes associés ({bank.accounts})</h3>
    {can('accounts.read')&&<><RelatedList bankId={bank.id}/>
    <Link className="mt-6 block accent-text" href="/bank-accounts">Gérer les comptes bancaires →</Link></>}
  </BankDialog>;
}
