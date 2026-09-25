'use client';
import { Archive } from 'lucide-react';
import { useApi } from '@/services/use-api';

export function ArchiveUsage({ clientId }: { clientId: string }) {
  const { data, error } = useApi<{ count: number; limit: number }>(`/clients/${clientId}/archive-usage`);
  if (error) return <p role="alert" className="danger-button">{error}</p>;
  if (!data) return <p className="muted text-sm">Chargement de l’archive client…</p>;
  const full = data.count >= data.limit;
  return <div className={`archive-usage ${full ? 'archive-full' : ''}`}>
    <Archive size={19} aria-hidden="true"/>
    <div className="flex-1"><p className="font-medium">{data.count} / {data.limit} relevés dans l’archive client</p><p className="text-xs muted mt-1">{full ? 'Limite atteinte. Supprimez un relevé avant d’en importer un nouveau.' : 'Chaque relevé importé compte dans cette capacité, quel que soit son statut.'}</p></div>
    <progress aria-label="Relevés conservés dans l’archive" value={Math.min(data.count, data.limit)} max={data.limit}/>
  </div>;
}
