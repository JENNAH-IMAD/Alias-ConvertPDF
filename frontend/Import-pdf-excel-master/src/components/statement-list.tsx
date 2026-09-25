'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { FormEvent, useState } from 'react';
import { ArchiveRestore, ArrowUpRight, FileCheck2, FileSpreadsheet, FileText, Pencil, Trash2 } from 'lucide-react';
import { useAuth } from '@/components/auth-context';
import { Dialog } from '@/components/dialog';
import { LoadState, Pagination } from '@/components/resource-editor';
import { StatementStatus } from '@/components/statement-status';
import { Button } from '@/components/ui/button';
import { AnimatedCard as Card } from '@/components/ui/card';
import { Page, request } from '@/services/api';
import { Statement, statusLabel } from '@/services/statements';
import { useApi } from '@/services/use-api';

type Props = { archived?: boolean; clientId?: string; accountId?: string; filters?: string; clientWorkspace?: boolean };
const processingStatuses = new Set(['QUEUED', 'ANALYZING', 'OCR_PROCESSING', 'EXPORTING']);

export function StatementList({ archived = false, clientId = '', accountId = '', filters = '', clientWorkspace = false }: Props) {
  const router = useRouter();
  const { can } = useAuth();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState('');
  const [deleting, setDeleting] = useState<Statement | null>(null);
  const [busyId, setBusyId] = useState('');
  const [actionError, setActionError] = useState('');
  const query = `/statements?page=${page}&pageSize=20&archived=${archived}&status=${status}${clientId ? '&clientId=' + clientId : ''}${accountId ? '&accountId=' + accountId : ''}${filters}`;
  const { data, error, loading, reload } = useApi<Page<Statement>>(query);
  const writable = can('statements.write');

  async function reopen(statement: Statement) {
    if (busyId) return;
    setBusyId(statement.id); setActionError('');
    try {
      await request(`/statements/${statement.id}/reopen`, { method: 'POST', body: JSON.stringify({ version: statement.version }) });
      router.push(`/statements/${statement.id}#review`);
    } catch (reason) { setActionError((reason as Error).message); }
    finally { setBusyId(''); }
  }

  async function remove(event: FormEvent) {
    event.preventDefault();
    if (!deleting || busyId) return;
    setBusyId(deleting.id); setActionError('');
    try {
      await request(`/statements/${deleting.id}`, { method: 'DELETE', body: JSON.stringify({ version: deleting.version }) });
      setDeleting(null);
      if (data?.items.length === 1 && page > 1) setPage(page - 1); else reload();
    } catch (reason) { setActionError((reason as Error).message); }
    finally { setBusyId(''); }
  }

  return <div className="ui-page">
    <div className="flex flex-wrap items-end justify-between gap-3">
      <div><h2 className="text-lg font-semibold">{clientWorkspace ? 'Tous les traitements de relevés' : archived ? 'Archives des relevés' : 'Relevés bancaires'}</h2>{clientWorkspace && <p className="ui-description">Relevés importés, en cours, à vérifier, validés, exportés, échoués et archivés.</p>}</div>
      <label className="text-sm">Statut<select className="ui-input mt-2" value={status} onChange={event => { setStatus(event.target.value); setPage(1); }}><option value="">Tous les statuts</option>{Object.entries(statusLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
    </div>
    <LoadState error={error || actionError} loading={loading} empty={!data?.items.length}/>
    <div className="grid gap-4 lg:grid-cols-2">{data?.items.map(statement => <Card key={statement.id} className="ui-card client-archive-card">
      <div className="flex flex-wrap items-start gap-3"><FileText className="shrink-0 accent-text" size={22}/><div className="min-w-0 flex-1"><h3 className="font-semibold break-all">{statement.originalFileName}</h3><p className="ui-description">{statement.client} · {statement.bank}</p></div><StatementStatus status={statement.status}/></div>
      <div className="ui-row"><span>Compte bancaire</span><span className="break-all">{statement.account}</span></div>
      <div className="ui-row"><span>Période du relevé</span><span>{statement.periodStart || 'À renseigner'} — {statement.periodEnd || '—'}</span></div>
      <div className="archive-file-summary"><span><FileText size={15}/>PDF original</span><span className={statement.transactionCount ? 'available' : 'unavailable'}><FileSpreadsheet size={15}/>{statement.transactionCount ? `Conversion · ${statement.transactionCount} opération(s)` : 'Conversion indisponible'}</span><span className={statement.exportCount ? 'available' : 'unavailable'}><FileCheck2 size={15}/>{statement.exportCount ? `${statement.exportCount} export(s) final(aux)` : 'Aucun export final'}</span></div>
      <div className="mt-4 flex flex-wrap items-center justify-between gap-3"><span className="muted text-xs">{statement.archivedAt ? 'Archivé' : 'Importé'} le {new Date(statement.archivedAt || statement.createdAt).toLocaleDateString('fr-FR')}</span><div className="document-actions">
        <Link className="button-secondary" href={`/statements/${statement.id}`}>Consulter</Link>
        <Link className="button-primary" href={`/statements/${statement.id}#files`}>Fichiers <ArrowUpRight size={15}/></Link>
        {clientWorkspace && writable && statement.archivedAt && <Button variant="ghost" className="button-secondary" disabled={busyId === statement.id} onClick={() => reopen(statement)}><ArchiveRestore size={15}/>{busyId === statement.id ? 'Réouverture…' : 'Rouvrir et modifier'}</Button>}
        {clientWorkspace && writable && !statement.archivedAt && <Link className="button-secondary" aria-disabled={processingStatuses.has(statement.status)} href={`/statements/${statement.id}#review`}><Pencil size={15}/>Modifier</Link>}
        {clientWorkspace && writable && <Button variant="ghost" className="button-secondary danger-button" disabled={busyId === statement.id || processingStatuses.has(statement.status)} onClick={() => { setActionError(''); setDeleting(statement); }}><Trash2 size={15}/>Supprimer</Button>}
      </div></div>
    </Card>)}</div>
    {data && <Pagination page={page} total={data.total} onChange={setPage}/>}
    {deleting && <Dialog title="Supprimer le relevé bancaire" onClose={() => setDeleting(null)} busy={busyId === deleting.id}><form onSubmit={remove} className="space-y-5"><p className="font-semibold break-all">{deleting.originalFileName}</p><p className="ui-description">Cette suppression est définitive. Le PDF original, les opérations extraites, l’historique et tous les fichiers exportés seront supprimés.</p>{actionError && <p role="alert" className="danger-button">{actionError}</p>}<div className="flex flex-wrap gap-3"><Button type="button" variant="ghost" className="button-secondary" disabled={Boolean(busyId)} onClick={() => setDeleting(null)}>Annuler</Button><Button type="submit" variant="destructive" disabled={Boolean(busyId)}>{busyId ? 'Suppression…' : 'Supprimer définitivement'}</Button></div></form></Dialog>}
  </div>;
}
