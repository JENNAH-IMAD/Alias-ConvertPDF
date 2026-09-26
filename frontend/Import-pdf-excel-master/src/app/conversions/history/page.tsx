'use client';
import { pageTheme } from '@/components/page-theme';

import { useTheme } from '@/components/theme-context';
import { useState } from 'react';
import { History, Page, download } from '@/services/api';
import { useApi } from '@/services/use-api';
import { LoadState, Pagination } from '@/components/resource-editor';

export default function ConversionHistoryPage() {
  const { isLightMode } = useTheme();
  const [page,setPage]=useState(1); const [search,setSearch]=useState('');const [status,setStatus]=useState(''); const [actionError,setActionError]=useState('');
  const {data,error,loading}=useApi<Page<History>>('/history?page='+page+'&pageSize=20&search='+encodeURIComponent(search)+'&status='+status);
  const conversionHistory=(data?.items||[]).map(h=>({...h,period:h.inputFileName,date:new Date(h.createdAt).toLocaleString('fr-FR')}));
  const save=async(h:History)=>{try{await download(h);}catch(e){setActionError((e as Error).message);}};
  const theme = pageTheme;

  return (
    <div className={theme.page}><LoadState error={error||actionError} loading={loading} empty={!conversionHistory.length}/>
      <section className="ui-hero">
        <p className={theme.label}>Historique</p>
        <h1 className={theme.title}>Historique des conversions</h1>
      </section>

      <section className={theme.card}>
        <div className="mb-5 grid gap-3 md:grid-cols-[1fr_200px]">
          <input type="text" aria-label="Recherche" value={search} onChange={e=>{setSearch(e.target.value);setPage(1);}} placeholder="Rechercher un client, banque ou fichier" className={theme.input} />
          <select aria-label="Statut" className={theme.select} value={status} onChange={e=>{setStatus(e.target.value);setPage(1);}}><option value="">Tout statut</option><option value="Completed">Terminé</option><option value="Pending">En attente</option><option value="Processing">En cours</option><option value="Failed">Échec</option></select>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full text-left text-sm">
            <thead><tr className={theme.head}><th className="pb-3 pr-4 font-medium">Client</th><th className="pb-3 pr-4 font-medium">Banque</th><th className="pb-3 pr-4 font-medium">Fichier</th><th className="pb-3 pr-4 font-medium">Date</th><th className="pb-3 pr-4 font-medium">Lignes</th><th className="pb-3 font-medium">Statut</th></tr></thead>
            <tbody>
              {conversionHistory.map((item) => (
                <tr key={item.id} className={theme.tableRow}>
                  <td className={`py-3 pr-4 font-medium ${isLightMode ? 'text-slate-800' : 'text-slate-100'}`}>{item.client}</td>
                  <td className={theme.cell}>{item.bank}</td>
                  <td className={theme.cell}>{item.period}</td>
                  <td className={theme.cell}>{item.date}</td>
                  <td className={theme.cell}>{item.count}</td>
                  <td className="py-3">
                    <span className={`${theme.status} ${
                      item.status === 'Completed'
                        ? (isLightMode ? 'bg-emerald-100 text-emerald-700' : 'bg-emerald-500/10 text-emerald-300')
                        : (item.status === 'Processing' || item.status === 'Pending')
                          ? (isLightMode ? 'bg-amber-100 text-amber-700' : 'bg-amber-500/10 text-amber-300')
                          : item.status === 'Généré'
                            ? (isLightMode ? 'bg-sky-100 text-sky-700' : 'bg-cyan-500/10 text-cyan-300')
                            : (isLightMode ? 'bg-rose-100 text-rose-700' : 'bg-rose-500/10 text-rose-300')
                    }`}>{{Pending:'En attente',Processing:'En cours',Completed:'Terminé',Failed:'Échec'}[item.status]||item.status}</span>{item.errorMessage&&<p className="mt-2 text-rose-500">{item.errorMessage}</p>}{item.status==='Completed'&&<button onClick={()=>save(item)} className="ml-3 text-sky-500">Télécharger</button>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section><Pagination page={page} total={data?.total||0} onChange={setPage}/>
    </div>
  );
}
