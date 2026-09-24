 'use client';
import { Button } from '@/components/ui/button';

import { useEffect, useState } from 'react';
import { Dialog as BankDialog } from './dialog';
import { request } from '@/services/api';
type Preview = { name: string; accounts: number; version: string };
export function CatalogDelete({ resource, id, name, onClose, onDeleted }: {
  resource: 'banks' | 'clients'; id: string; name: string; onClose: () => void; onDeleted: () => void;
}) {
  const [preview, setPreview] = useState<Preview | null>(null);
  const [error, setError] = useState(''); const [busy, setBusy] = useState(false); const [confirmed, setConfirmed] = useState(false);
  const path = `/catalog-deletions/${resource}/${id}`;
  useEffect(() => { let active = true; request<Preview>(path).then(p => { if (active) setPreview(p); }).catch(e => { if (active) setError(e.message); }); return () => { active = false; }; }, [path]);
  async function remove() {
    if (!preview || !confirmed || busy) return;
    setBusy(true); setError('');
    try { await request(path, { method: 'POST', body: JSON.stringify({ version: preview.version }) }); onDeleted(); onClose(); }
    catch (e) { setError((e as Error).message); setConfirmed(false); }
    finally { setBusy(false); }
  }
  return <BankDialog title={resource === 'banks' ? 'Supprimer la banque' : 'Supprimer le client'} onClose={onClose} busy={busy}>
    <p className="font-semibold mb-4">{preview?.name || name}</p>
    {!preview && !error && <p role="status">Vérification des comptes associés…</p>}
    {preview && <><p className="ui-description">Cette suppression est définitive. {preview.accounts} compte(s) bancaire(s) associé(s) seront également supprimés.</p><p className="ui-description">{resource === 'banks' ? 'Les clients sont conservés.' : 'Les banques sont conservées.'}</p><label className="flex items-start gap-3 mt-5 text-sm"><input type="checkbox" checked={confirmed} disabled={busy} onChange={e => setConfirmed(e.target.checked)}/>Je confirme la suppression de cet élément et de ses comptes associés.</label></>}
    {error && <p role="alert" className="danger-button mt-4">{error}</p>}
    <div className="flex justify-end gap-3 mt-6"><Button variant="ghost" className="button-secondary" disabled={busy} onClick={onClose}>Annuler</Button><Button variant="ghost" className="button-secondary danger-button" disabled={!preview || !confirmed || busy} onClick={remove}>{busy ? 'Suppression…' : 'Supprimer définitivement'}</Button></div>
  </BankDialog>;
}
