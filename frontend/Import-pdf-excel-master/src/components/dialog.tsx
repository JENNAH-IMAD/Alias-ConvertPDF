'use client';
import { Button } from '@/components/ui/button';

import { ReactNode, useEffect, useRef, useId } from 'react';
import { motion, useReducedMotion } from 'motion/react';
export function Dialog({ title, children, onClose, busy = false, wide = false }: { title: string; children: ReactNode; onClose: () => void; busy?: boolean; wide?: boolean }) {
  const titleId = useId();
  const reduced = useReducedMotion();
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => { const dialog = ref.current; dialog?.showModal(); return () => dialog?.close(); }, []);
  return <motion.dialog initial={reduced ? false : { opacity: 0, y: 12, scale: 0.98 }} animate={{ opacity: 1, y: 0, scale: 1 }} transition={{ duration: reduced ? 0 : 0.2 }} ref={ref} aria-labelledby={titleId} onCancel={e => { e.preventDefault(); if (!busy) onClose(); }} className={`app-dialog ${wide ? 'wide-dialog' : ''}`}>
    <div className="mb-6 flex items-start justify-between gap-4"><h2 id={titleId} className="text-xl font-semibold">{title}</h2><Button variant="ghost" type="button" disabled={busy} onClick={onClose} className="rounded-lg border border-current/20 px-3 py-1.5 disabled:opacity-40">Fermer</Button></div>{children}
  </motion.dialog>;
}
