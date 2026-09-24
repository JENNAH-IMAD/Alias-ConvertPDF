'use client';

import { createContext, useContext, type ReactNode } from 'react';
import { motion, useReducedMotion } from 'motion/react';
import { PanelLeftClose, PanelLeftOpen } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from './button';
import { Tooltip, TooltipContent, TooltipTrigger } from './tooltip';

const SidebarContext = createContext<{ open: boolean; onToggle: () => void } | null>(null);

function useSidebar() {
  const context = useContext(SidebarContext);
  if (!context) throw new Error('Sidebar components require Sidebar');
  return context;
}

export function Sidebar({ open, onToggle, children }: { open: boolean; onToggle: () => void; children: ReactNode }) {
  return <SidebarContext.Provider value={{ open, onToggle }}>{children}</SidebarContext.Provider>;
}

/** shadcn Button + Tooltip, composed with Motion for the clickable trigger. */
export function SidebarTrigger() {
  const { open, onToggle } = useSidebar();
  const reducedMotion = useReducedMotion();
  const label = open ? 'Réduire le menu' : 'Développer le menu';
  return <Tooltip><TooltipTrigger asChild>
    <Button type="button" size="icon" variant="ghost" className="icon-button sidebar-toggle"
      aria-label={label} aria-expanded={open} aria-controls="desktop-navigation" onClick={onToggle}>
      <motion.span key={String(open)} className="sidebar-trigger-icon" initial={reducedMotion ? false : { opacity: 0, rotate: -15, scale: 0.8 }}
        animate={{ opacity: 1, rotate: 0, scale: 1 }} transition={{ duration: reducedMotion ? 0 : 0.18 }}>
        {open ? <PanelLeftClose size={20}/> : <PanelLeftOpen size={20}/>}
      </motion.span>
    </Button>
  </TooltipTrigger><TooltipContent side="bottom" className="navigation-tooltip">{label}</TooltipContent></Tooltip>;
}

export function DesktopSidebar({ children, className }: { children: ReactNode; className?: string }) {
  const { open } = useSidebar();
  const reducedMotion = useReducedMotion();
  return <motion.aside id="desktop-navigation" data-state={open ? 'expanded' : 'collapsed'}
    className={cn('desktop-sidebar', className)} initial={false}
    animate={{ width: open ? 'var(--sidebar-expanded)' : '84px' }}
    transition={{ duration: reducedMotion ? 0 : 0.24, ease: [0.22, 1, 0.36, 1] }}>
    {children}
  </motion.aside>;
}
