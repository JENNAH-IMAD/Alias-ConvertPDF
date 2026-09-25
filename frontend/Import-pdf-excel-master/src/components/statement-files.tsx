'use client';
import { useEffect, useState } from 'react';
import { Download, Eye, FileText, FileSpreadsheet, FileCheck2 } from 'lucide-react';
import { Detail, download, fileBlob } from '@/services/statements';
import { useApi } from '@/services/use-api';
import { Button } from '@/components/ui/button';
import { Dialog } from '@/components/dialog';
import { LoadState } from '@/components/resource-editor';

function PdfPreview({ path }: { path: string }) {
  const [url, setUrl] = useState(''); const [error, setError] = useState('');
  useEffect(() => {
    const controller = new AbortController(); let objectUrl = '';
    fileBlob(path, controller.signal).then(blob => {
      if (controller.signal.aborted) return;
      objectUrl = URL.createObjectURL(new Blob([blob], { type: 'application/pdf' })); setUrl(objectUrl);
    }).catch(e => { if (!controller.signal.aborted) setError(e.message); });
    return () => { controller.abort(); if (objectUrl) URL.revokeObjectURL(objectUrl); };
  }, [path]);
  if (!url) return <LoadState error={error} loading={!error}/>;
  return <><iframe src={url} title="Consultation du PDF original" className="pdf-document-view"/><p className="ui-description mt-3">Si votre navigateur ne permet pas l’aperçu, utilisez Télécharger.</p></>;
}
function TextPreview({ path }: { path: string }) {
  const { data, error, loading } = useApi<{ text: string; truncated?: boolean; limitedTo?: number }>(path);
  if (!data) return <LoadState error={error} loading={loading}/>;
  return <><p className="ui-description mb-3">{data.limitedTo ? `Aperçu des ${data.limitedTo} premières opérations enregistrées.` : data.truncated ? 'Aperçu limité à 32 000 caractères. Le téléchargement contient le fichier complet.' : 'Contenu du fichier final conservé, sans nouvelle conversion.'}</p><pre className="document-text-view">{data.text}</pre></>;
}
type Viewer = { title: string; path: string; downloadPath: string; name: string; pdf?: boolean };
export function StatementFiles({ detail: s }: { detail: Detail }) {
  const [viewer, setViewer] = useState<Viewer | null>(null); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  const pdfPath = `/statements/${s.id}/pdf`; const convertedPath = `/statements/${s.id}/converted`;
  const convertedName = `Releve_converti_${s.id}_${s.validatedAt ? 'valide' : 'a_verifier'}.csv`;
  const processing = ['QUEUED', 'ANALYZING', 'OCR_PROCESSING'].includes(s.status);
  async function save(path: string, name: string) { setBusy(true); setError(''); try { await download(path, name); } catch (e) { setError((e as Error).message); } finally { setBusy(false); } }
  return <section id="files" className="ui-card scroll-mt-24">
    <p className="ui-eyebrow">DOCUMENTS DU RELEVÉ</p><h2 className="text-xl font-semibold mt-2">Consulter et télécharger</h2>
    {error && <p className="danger-button mt-3" role="alert">{error}</p>}
    <div className="document-grid mt-5">
      <div className="document-card"><FileText size={25} className="accent-text"/><h3>PDF original</h3><p>Le document importé, conservé sans modification.</p><div className="document-actions">
        <Button variant="outline" onClick={() => setViewer({ title: 'PDF original', path: pdfPath, downloadPath: pdfPath, name: s.originalFileName, pdf: true })}><Eye/>Consulter</Button>
        <Button variant="outline" disabled={busy} onClick={() => save(pdfPath, s.originalFileName)}><Download/>Télécharger</Button>
      </div></div>
      <div className="document-card"><FileSpreadsheet size={25} className="accent-text"/><h3>Fichier converti</h3><p>CSV de travail des opérations enregistrées. {s.validatedAt ? 'Données validées.' : 'À vérifier avant utilisation comptable.'}</p><div className="document-actions">
        <Button variant="outline" disabled={!s.transactions.length || processing} onClick={() => setViewer({ title: 'Fichier converti · CSV de travail', path: convertedPath + '/preview', downloadPath: convertedPath + '/download', name: convertedName })}><Eye/>Consulter</Button>
        <Button variant="outline" disabled={busy || !s.transactions.length || processing} onClick={() => save(convertedPath + '/download', convertedName)}><Download/>Télécharger</Button>
      </div></div>
      <div className="document-card"><FileCheck2 size={25} className="accent-text"/><h3>Exports finaux</h3><p>{s.exports.length ? `${s.exports.length} fichier(s) généré(s) et conservé(s). Retrouvez chaque version ci-dessous.` : 'Après validation, générez le fichier Sage 100, Sage X3 ou CSV choisi.'}</p><span className="ui-tag justify-self-start">{s.exports.length} fichier(s)</span></div>
    </div>
    <div className="document-exports">{s.exports.map(e => <div className="document-export-row" key={e.id}><div className="min-w-0"><strong className="block break-all text-sm">{e.fileName}</strong><small className="muted">{new Date(e.createdAt).toLocaleString('fr-FR')} · {(e.fileSize / 1024).toFixed(1)} Ko</small></div><div className="document-actions">
      <Button variant="outline" onClick={() => setViewer({ title: 'Export final', path: `/statement-exports/${e.id}/preview`, downloadPath: `/statement-exports/${e.id}/download`, name: e.fileName })}><Eye/>Consulter</Button>
      <Button variant="outline" disabled={busy} onClick={() => save(`/statement-exports/${e.id}/download`, e.fileName)}><Download/>Télécharger</Button>
    </div></div>)}</div>
    {viewer && <Dialog title={viewer.title} onClose={() => setViewer(null)} wide><div className="flex flex-wrap items-center justify-between gap-3 mb-4"><p className="text-sm break-all min-w-0">{viewer.name}</p><Button variant="outline" disabled={busy} onClick={() => save(viewer.downloadPath, viewer.name)}><Download/>Télécharger</Button></div>{error && <p role="alert" className="danger-button mb-3">{error}</p>}{viewer.pdf ? <PdfPreview path={viewer.path}/> : <TextPreview path={viewer.path}/>}</Dialog>}
  </section>;
}
