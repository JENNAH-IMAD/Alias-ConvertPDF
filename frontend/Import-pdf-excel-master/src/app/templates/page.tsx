'use client';
import { pageTheme } from '@/components/page-theme';

import { useTheme } from '@/components/theme-context';
import { useState } from 'react';
import { useAuth } from '@/components/auth-context';
import { ResourceEditor, LoadState, Pagination } from '@/components/resource-editor';
import { ExportTemplate, Page, request } from '@/services/api';
import { useApi } from '@/services/use-api';

export default function TemplatesPage() {
  const { isLightMode } = useTheme();
  const { user } = useAuth(); const canEdit = user?.role === 'Admin';
  const [page,setPage] = useState(1);
  const {data,error,loading,reload} = useApi<Page<ExportTemplate>>('/export-templates?page='+page+'&pageSize=20');
  const templates = data?.items || [];
  const [editing,setEditing] = useState<ExportTemplate | null | undefined>(undefined);
  const [actionError,setActionError] = useState('');
  const remove = async (item:ExportTemplate) => {if(!confirm('Supprimer cet élément ?'))return;try{await request('/export-templates/'+item.id,{method:'DELETE'});reload();}catch(e){setActionError((e as Error).message);}};
  const theme = pageTheme;

  return (
    <div className={theme.page}>
      <LoadState error={error||actionError} loading={loading} empty={!templates.length}/>
      {editing !== undefined && <ResourceEditor resource="export-templates" initial={editing||undefined} onClose={()=>setEditing(undefined)} onSaved={reload}/>}
      <section className={theme.hero}>
        <div className="flex flex-col gap-5 md:flex-row md:items-center md:justify-between">
          <div>
            <p className={theme.chip}>Formats de sortie</p>
            <h1 className={theme.title}>Modèles d’export</h1>
          </div>
          {canEdit && <button className={theme.button} onClick={()=>setEditing(null)}>Nouveau modèle</button>}
        </div>
      </section>

      <section className="grid gap-5 xl:grid-cols-2">
        {templates.map((template) => (
          <article key={template.id} className={theme.card}>
            <div className="mb-4 flex items-center justify-between gap-3">
              <div>
                <p className={isLightMode ? 'text-lg font-semibold text-slate-900' : 'text-lg font-semibold text-white'}>{template.name}</p>
                <p className={isLightMode ? 'text-sm text-slate-500' : 'text-sm text-slate-400'}>{template.type}</p>
              </div>
              <span className="ui-tag accent-text">{template.fileExtension.toUpperCase()}</span>
            </div>

            <div className={`space-y-3 text-sm ${isLightMode ? 'text-slate-600' : 'text-slate-300'}`}>
              <div className={theme.row}><span className={theme.rowLabel}>Délimiteur</span><span className={theme.rowValue}>{template.delimiter}</span></div>
              <div className={theme.row}><span className={theme.rowLabel}>Encodage</span><span className={theme.rowValue}>{template.encoding}</span></div>
            </div>

            <div className="mt-5">
              <p className={`mb-2 text-[11px] font-medium uppercase tracking-[0.2em] ${isLightMode ? 'text-slate-500' : 'text-slate-400'}`}>Champs</p>
              <div className="flex flex-wrap gap-2">
                {template.fields.map((field) => <span key={field.position} className={theme.tag}>{field.fieldName}</span>)}
              </div>
            </div>
          {canEdit && <div className="mt-4 flex gap-4 text-sm"><button className="button-secondary" onClick={()=>setEditing(template)}>Modifier</button><button className="button-secondary danger-button" onClick={()=>remove(template)}>Supprimer</button></div>}</article>
        ))}
      </section><Pagination page={page} total={data?.total||0} onChange={setPage}/>
    </div>
  );
}
