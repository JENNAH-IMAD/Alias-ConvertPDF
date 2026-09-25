'use client';
import { useState } from 'react';
import Link from 'next/link';
import { FolderOpen, ListChecks, Upload } from 'lucide-react';
import { useCatalog } from '@/services/use-catalog';
import { Client, Account } from '@/services/api';
import { useAuth } from '@/components/auth-context';
import { ArchiveUsage } from '@/components/archive-usage';
import { LoadState } from '@/components/resource-editor';
import { StatementList } from '@/components/statement-list';

export default function ClientSpace() {
  const clients = useCatalog<Client>('/clients');
  const accounts = useCatalog<Account>('/bank-accounts');
  const { can } = useAuth();
  const [id, setId] = useState(''); const [account, setAccount] = useState('');
  const client = clients.data?.items.find(c => c.id === id);
  return <div className="ui-page">
    <section className="ui-hero"><p className="ui-eyebrow">DOSSIERS CLIENTS</p><h1 className="ui-title">Espace clients</h1><p className="ui-description">Suivez tous les traitements de relevés bancaires du client, de l’import à l’archivage.</p></section>
    <LoadState error={clients.error || accounts.error} loading={clients.loading || accounts.loading}/>
    <section className="ui-card"><h2 className="flex items-center gap-2 font-semibold mb-4"><FolderOpen size={20}/>Ouvrir un dossier client</h2><div className="grid gap-4 sm:grid-cols-2">
      <label>Client<select className="ui-input mt-2" value={id} onChange={e => { setId(e.target.value); setAccount(''); }}><option value="">Sélectionner un client</option>{clients.data?.items.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}</select></label>
      <label>Compte bancaire<select className="ui-input mt-2" disabled={!id} value={account} onChange={e => setAccount(e.target.value)}><option value="">Tous les comptes du client</option>{accounts.data?.items.filter(a => a.clientId === id).map(a => <option key={a.id} value={a.id}>{a.bank} · {a.accountNumber} · {a.currency}</option>)}</select></label>
    </div></section>
    {client ? <>
      <section className="ui-card"><div className="flex flex-wrap items-center justify-between gap-4"><div><h2 className="text-lg font-semibold">{client.name}</h2><p className="ui-description">{client.legalName || 'Dossier client'}</p></div>{can('statements.write') && <Link href="/conversions" className="button-primary"><Upload size={16}/>Importer un relevé</Link>}</div>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 mt-4">{[['ICE', client.ice], ['IF', client.if], ['RC', client.rc], ['E-mail', client.email], ['Téléphone', client.phone], ['Pays', client.country]].map(([label, value]) => <div className="ui-row" key={label}><span className="muted">{label}</span><span className="break-all">{value || '—'}</span></div>)}</div><p className="ui-description mt-3">Banques : {client.banks.join(', ') || 'Aucune'}</p>
      </section>
      <ArchiveUsage key={`archive-usage-${id}`} clientId={id}/>
      <div className="client-archive-heading"><ListChecks size={21}/><div><h2>Traitements des relevés bancaires</h2><p>Consultez les fichiers et leur historique, corrigez les traitements ou supprimez un relevé.</p></div></div>
      <StatementList key={`client-statements-${id}-${account || 'all'}`} clientId={id} accountId={account} clientWorkspace/>
    </> : !clients.loading && <div className="load-state"><FolderOpen size={22}/><p>Sélectionnez un client pour consulter tous ses traitements de relevés bancaires.</p></div>}
  </div>;
}
