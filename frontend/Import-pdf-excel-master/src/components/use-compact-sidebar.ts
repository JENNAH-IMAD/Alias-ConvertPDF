'use client';

import { useSyncExternalStore } from 'react';
const key = 'alias-sidebar-compact';
let memoryValue = false;
function subscribe(listener: () => void) {
  window.addEventListener('storage', listener);
  window.addEventListener('sidebar-change', listener);
  return () => { window.removeEventListener('storage', listener); window.removeEventListener('sidebar-change', listener); };
}
function snapshot() {
  try { return localStorage.getItem(key) === 'true'; } catch { return memoryValue; }
}
export function useCompactSidebar() {
  const compact = useSyncExternalStore(subscribe, snapshot, () => false);
  const toggle = () => {
    memoryValue = !compact;
    try { localStorage.setItem(key, String(memoryValue)); } catch { /* Private browsing may restrict storage. */ }
    window.dispatchEvent(new Event('sidebar-change'));
  };
  return { compact, toggle };
}
