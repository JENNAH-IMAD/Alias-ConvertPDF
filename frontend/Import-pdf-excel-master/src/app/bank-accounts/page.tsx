'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { BlurFade } from '@/components/ui/blur-fade';
import { ArrowUpRight, BookOpen, Check, Copy, CreditCard, Landmark, LayoutGrid, List, Pencil, Plus, RefreshCw, Trash2, Wallet } from 'lucide-react';
import { useAuth } from '@/components/auth-context';
import { Dialog } from '@/components/dialog';
import { ResourceEditor, LoadState, Pagination } from '@/components/resource-editor';
import { Account, Page, request } from '@/services/api';
import { useApi } from '@/services/use-api';

export default function BankAccountsPage() {
  const { can } = useAuth();
  const [page, setPage] = useState(1);
  const { data, error, loading, reload } = useApi<Page<Account>>(`/bank-accounts?page=${page}&pageSize=20`);
  const [editing, setEditing] = useState<Account | null | undefined>(undefined);
  const [deleting, setDeleting] = useState<Account | null>(null);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState('');
  const [view, setView] = useState<'cards' | 'list'>('cards');
  const [copied, setCopied] = useState('');
  const items = data?.items ?? [];

  async function remove() {
    if (!deleting || busy) return;
    setBusy(true); setActionError('');
    try {
      await request(`/bank-accounts/${deleting.id}`, { method: 'DELETE' });
      setDeleting(null);
      if (items.length === 1 && page > 1) setPage(page - 1);
      else reload();
    } catch (e) { setActionError((e as Error).message); }
    finally { setBusy(false); }
  }

  async function copy(account: Account) {
    try { await navigator.clipboard.writeText(account.accountNumber); setCopied(account.id); }
    catch { setActionError('La copie est indisponible. Vous pouvez sélectionner le numéro du compte.'); }
  }

  return <div className="ui-page accounts-page">
    <section className="accounts-heading">
      <div><p className="ui-eyebrow">Votre référentiel bancaire</p><h1 className="ui-title">Comptes bancaires<span className="title-dot">.</span></h1><p className="ui-description">Chaque compte à sa place. Toute votre organisation, en un regard.</p></div>
      {can('accounts.write') && <Button className="button-primary" onClick={() => setEditing(null)}><Plus size={17}/>Nouveau compte</Button>}
    </section>

    <section className="accounts-overview" aria-label="Vue d’ensemble des comptes">
      <div className="account-stat account-stat-primary"><div className="stat-top"><span>Comptes enregistrés</span><Wallet size={21}/></div><strong>{loading || error ? '—' : data?.total ?? 0}</strong><p>Votre référentiel de comptes bancaires</p><span className="stat-decoration" aria-hidden="true"/></div>
      <div className="account-stat"><div className="stat-top"><span>Banques représentées</span><Landmark size={20}/></div><strong>{loading || error ? '—' : new Set(items.map(a => a.bankId)).size}</strong><p>Sur cette page de comptes</p></div>
      <div className="account-stat"><div className="stat-top"><span>Devises utilisées</span><CreditCard size={20}/></div><strong>{loading || error ? '—' : new Set(items.map(a => a.currency)).size}</strong><p>{items.length ? [...new Set(items.map(a => a.currency))].join(' · ') : 'Sur cette page de comptes'}</p></div>
    </section>

    <section className="accounts-collection" aria-labelledby="accounts-list-title">
      <div className="accounts-toolbar"><div><h2 id="accounts-list-title">Vos comptes <span className="count-badge">{data?.total ?? '—'}</span></h2><p className="muted">Coordonnées bancaires et affectation comptable</p></div><div className="account-toolbar-actions"><Button variant="ghost" className="icon-button" aria-label="Actualiser les comptes" disabled={loading} onClick={() => { setActionError(''); setCopied(''); reload(); }}><RefreshCw size={17}/></Button><div className="view-switch" role="group" aria-label="Affichage des comptes"><Button variant="ghost" aria-label="Vue en cartes" aria-pressed={view === 'cards'} onClick={() => setView('cards')}><LayoutGrid size={17}/></Button><Button variant="ghost" aria-label="Vue en liste" aria-pressed={view === 'list'} onClick={() => setView('list')}><List size={19}/></Button></div></div></div>
      <LoadState error={error || (!deleting ? actionError : '')} loading={loading}/>
      {!loading && !error && !items.length && <div className="accounts-empty"><span className="empty-account-icon"><Wallet size={32}/></span><h3>Votre premier compte commence ici</h3><p>Reliez un client à sa banque et renseignez ses coordonnées comptables.</p>{can('accounts.write') && <Button className="button-primary" onClick={() => setEditing(null)}><Plus size={16}/>Ajouter un compte</Button>}</div>}
      {!loading && !error && items.length > 0 && <div className={`accounts-grid ${view === 'list' ? 'accounts-list' : ''}`}>
        {items.map((account, index) => <BlurFade key={account.id} delay={Math.min(index, 5) * 0.035} duration={0.28} blur="2px"><article className="bank-account-card">
          <div className="account-card-top"><span className="bank-emblem"><Landmark size={22} strokeWidth={1.6}/></span><div className="account-identity"><p>{account.bank}</p><h3>{account.accountName}</h3></div><span className="currency-badge">{account.currency}</span></div>
          <div className="account-number-block"><span className="account-field-label">Numéro de compte</span><div><span className="account-number">{account.accountNumber}</span><Button variant="ghost" className="copy-account" aria-label={copied === account.id ? 'Numéro copié' : `Copier le numéro de ${account.accountName}`} onClick={() => copy(account)}>{copied === account.id ? <Check size={16}/> : <Copy size={16}/>}</Button></div></div>
          <dl className="account-ledger"><div><dt><BookOpen size={14}/>Journal</dt><dd>{account.journal}</dd></div><div><dt>Compte comptable</dt><dd>{account.accountCode}</dd></div></dl>
          <div className="account-card-actions"><span className="account-record-label"><CreditCard size={14}/>Compte bancaire</span><div>{can('accounts.write') && <Button variant="ghost" className="account-edit" onClick={() => setEditing(account)} aria-label={`Modifier ${account.accountName}`}><Pencil size={14}/>Modifier<ArrowUpRight size={14}/></Button>}{can('accounts.delete') && <Button variant="ghost" className="icon-button danger-button" aria-label={`Supprimer ${account.accountName}`} onClick={() => { setActionError(''); setDeleting(account); }}><Trash2 size={15}/></Button>}</div></div>
        </article></BlurFade>)}
      </div>}
      {!loading && !error && !!data?.total && <Pagination page={page} total={data.total} onChange={p => { setCopied(''); setPage(p); }}/>}
      <span className="sr-only" role="status">{copied ? 'Numéro de compte copié dans le presse-papiers.' : ''}</span>
    </section>
    {editing !== undefined && <ResourceEditor resource="bank-accounts" initial={editing || undefined} onClose={() => setEditing(undefined)} onSaved={reload}/>}
    {deleting && <Dialog title="Supprimer ce compte ?" onClose={() => setDeleting(null)} busy={busy}><p className="ui-description">Le compte <strong>{deleting.accountName}</strong> de {deleting.bank} sera supprimé. Cette action est définitive.</p><div className="ui-summary my-5 account-number">{deleting.accountNumber}</div>{actionError && <p role="alert" className="danger-button mb-4">{actionError}</p>}<div className="flex justify-end gap-3"><Button variant="ghost" className="button-secondary" disabled={busy} onClick={() => setDeleting(null)}>Annuler</Button><Button variant="ghost" className="button-secondary danger-button" disabled={busy} onClick={remove}><Trash2 size={16}/>{busy ? 'Suppression…' : 'Supprimer le compte'}</Button></div></Dialog>}
  </div>;
}
