import { Archive, CheckCircle2, Clock3, FileText, CircleAlert, LoaderCircle } from 'lucide-react';
import { statusLabel } from '@/services/statements';

export function StatementStatus({ status }: { status: string }) {
  const pending = ['QUEUED', 'ANALYZING', 'OCR_PROCESSING'].includes(status);
  const failed = status.endsWith('_FAILED');
  const success = ['VALIDATED', 'EXPORTED'].includes(status);
  const review = ['UNKNOWN_FORMAT', 'REVIEW_REQUIRED'].includes(status);
  const tone = failed ? 'error' : success ? 'success' : pending ? 'progress' : review ? 'warning' : 'neutral';
  const Icon = status === 'ARCHIVED' ? Archive : failed || review ? CircleAlert : success ? CheckCircle2 : status === 'QUEUED' ? Clock3 : pending ? LoaderCircle : FileText;
  return <span className={`statement-status status-${tone}`}><Icon size={14} aria-hidden="true" className={pending && status !== 'QUEUED' ? 'status-spinner' : ''}/>{statusLabel[status] || 'Statut indisponible'}</span>;
}
