'use client';
import { AnimatedCard as Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';

import { ClientAvatar, ClientPhotoEditor } from '@/components/client-photo';
import { Camera } from 'lucide-react';
import { pageTheme } from '@/components/page-theme';

import { useTheme } from '@/components/theme-context';
import { useEffect, useState } from 'react';
import { useAuth } from '@/components/auth-context';
import { BankDelete, BankDetails, BankEditor } from '@/components/bank-dialogs';
import { Bank, Page } from '@/services/api';
import { useApi } from '@/services/use-api';

export default function BanksPage() {
  const { isLightMode } = useTheme();
  const { can } = useAuth(); const canEdit = can('banks.write');
  const [page,setPage] = useState(1);
  const [search, setSearch] = useState(''); const [term, setTerm] = useState('');
  const [sort, setSort] = useState('name');
  const [notice, setNotice] = useState('');
  useEffect(() => { const timer = setTimeout(() => { setTerm(search); setPage(1); }, 300); return () => clearTimeout(timer); }, [search]);
  const {data,error,loading,reload} = useApi<Page<Bank>>(`/banks?page=${page}&pageSize=20&search=${encodeURIComponent(term)}&sort=${sort}`);
  const bankRegistry = data?.items || [];
  const [editing,setEditing] = useState<Bank | null | undefined>(undefined);
  const [deleting, setDeleting] = useState<Bank | null>(null);
  const [logoBank,setLogoBank] = useState<Bank|null>(null);
  const [details, setDetails] = useState<Bank | null>(null);
  const theme = pageTheme;

  return (
    <div className={theme.page}>
      {logoBank && <ClientPhotoEditor resource="banks" client={{...logoBank,photoVersion:logoBank.logoVersion}} onClose={()=>setLogoBank(null)} onSaved={()=>{reload();setNotice("Logo enregistré.");}}/>}
      {editing !== undefined && <BankEditor initial={editing} onClose={()=>setEditing(undefined)} onSaved={(saved)=>{if(!editing)setLogoBank(saved);setNotice(editing ? 'Banque modifiée avec succès.' : 'Banque ajoutée avec succès.'); reload();}}/>}
      {deleting && <BankDelete bank={deleting} onClose={()=>setDeleting(null)} onDeleted={()=>{setNotice('Banque supprimée.'); if(bankRegistry.length===1&&page>1)setPage(page-1);else reload();}}/>}
      {details && <BankDetails bank={details} onClose={()=>setDetails(null)}/>}
      <section className={theme.hero}>
        <div className="flex flex-col gap-5 md:flex-row md:items-center md:justify-between">
          <div>
            <p className={theme.chip}>Référentiel</p>
            <h1 className={theme.title}>Banques</h1>
            <p className={`mt-3 ${theme.meta}`}>Gérez vos banques, consultez leurs comptes associés.</p>
          </div>
          {canEdit && <Button variant="ghost" className={theme.button} onClick={()=>setEditing(null)}>Ajouter une banque</Button>}
        </div>
      </section>

      {!canEdit && <p className="rounded-xl border read-only-notice p-4 text-sm">Consultation seule. Vos permissions ne permettent pas de modifier les banques.</p>}
      {notice && <div role="status" className="flex justify-between gap-4 rounded-xl success-notice p-4"><span>{notice}</span><Button variant="ghost" aria-label="Fermer le message" onClick={()=>setNotice('')}>×</Button></div>}
      <section className={`${theme.card} flex flex-wrap items-end gap-4`} aria-label="Recherche et filtres">
        <label className="min-w-48 flex-1 text-sm">Rechercher<Input type="search" maxLength={200} value={search} onChange={e=>setSearch(e.target.value)} placeholder="Nom, code ou description…" className="ui-input mt-2"/></label>
        <label className="text-sm">Trier par<select value={sort} onChange={e=>{setSort(e.target.value);setPage(1);}} className="ui-input mt-2"><option value="name">Nom A–Z</option><option value="name-desc">Nom Z–A</option><option value="code">Code</option></select></label>
        <Button variant="ghost" onClick={reload} disabled={loading} className="rounded-xl border border-current/20 px-4 py-2.5 text-sm disabled:opacity-40">Actualiser</Button>
      </section>
      {loading && <p role="status">Chargement des banques…</p>}
      {error && <div role="alert" className="rounded-xl bg-rose-500/10 p-4 text-rose-500">{error} <Button variant="ghost" className="ml-3 underline" onClick={reload}>Réessayer</Button></div>}
      {!loading && !error && !bankRegistry.length && <section className={`${theme.card} text-center`}><h2 className="text-lg font-semibold">{term?'Aucune banque ne correspond aux filtres.':'Aucune banque enregistrée.'}</h2><p className={`mt-2 ${theme.meta}`}>{term?'Modifiez votre recherche ou réinitialisez les filtres.':'Ajoutez votre première banque pour y associer des comptes.'}</p>{(term)&&<Button variant="ghost" className="mt-4 accent-text" onClick={()=>{setSearch('');setTerm('');setPage(1);}}>Réinitialiser les filtres</Button>}</section>}
      <section className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
        {bankRegistry.map((bank) => (
          <Card asChild key={bank.id}><article className={theme.card}>
            <div className="mb-4 flex items-center justify-between gap-3">
              <div className="flex items-center gap-3">
                <ClientAvatar resource="banks" client={{...bank,photoVersion:bank.logoVersion}}/>
                <p className={isLightMode ? 'text-lg font-semibold text-slate-900' : 'text-lg font-semibold text-white'}>{bank.name}</p>
              </div>
            </div>

            <div className={`space-y-3 text-sm ${theme.meta}`}>
              <div className={theme.row}><span className={theme.rowLabel}>Clients</span><span className={theme.rowValue}>{bank.clients}</span></div>
              <div className={theme.row}><span className={theme.rowLabel}>Code bancaire</span><span className={theme.rowValue}>{bank.code}</span></div>
              <div className={theme.row}><span className={theme.rowLabel}>Comptes</span><span className={theme.rowValue}>{bank.accounts}</span></div>
              
              <p className="line-clamp-2 break-words pt-1">{bank.description || 'Aucune description.'}</p>
            </div>
          <div className="mt-5 flex flex-wrap gap-2 border-t border-current/10 pt-4 text-sm"><Button variant="ghost" className="button-secondary" onClick={()=>setDetails(bank)}>Détails</Button>{canEdit && <><Button variant="ghost" className="button-secondary" onClick={()=>setLogoBank(bank)}><Camera size={15}/> {bank.logoVersion?"Modifier le logo":"Ajouter un logo"}</Button><Button variant="ghost" className="button-secondary accent-text" onClick={()=>setEditing(bank)}>Modifier</Button></>}{can('banks.delete')&&can('accounts.delete')&&<Button variant="ghost" className="button-secondary danger-button" onClick={()=>setDeleting(bank)}>Supprimer</Button>}</div></article></Card>
        ))}
      </section>{data && <nav aria-label="Pagination des banques" className="flex flex-wrap items-center justify-between gap-4 text-sm"><p>{data.total} banque(s) · Page {page} sur {Math.max(1,Math.ceil(data.total/20))}</p><div className="flex gap-3"><Button variant="ghost" disabled={page===1||loading} className="rounded-xl border border-current/20 px-4 py-2 disabled:cursor-not-allowed disabled:opacity-30" onClick={()=>setPage(page-1)}>Précédent</Button><Button variant="ghost" disabled={page*20>=data.total||loading} className="rounded-xl border border-current/20 px-4 py-2 disabled:cursor-not-allowed disabled:opacity-30" onClick={()=>setPage(page+1)}>Suivant</Button></div></nav>}
    </div>
  );
}
