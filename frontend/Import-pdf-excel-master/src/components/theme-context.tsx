'use client';

import { createContext, useContext, useSyncExternalStore } from 'react';

interface ThemeContextValue {
  isLightMode: boolean;
  toggleTheme: () => void;
}

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

function subscribe(listener: () => void) {
  window.addEventListener('storage', listener);
  window.addEventListener('theme-change', listener);
  return () => { window.removeEventListener('storage', listener); window.removeEventListener('theme-change', listener); };
}

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const isLightMode = useSyncExternalStore(subscribe, () => localStorage.getItem('bank-converter-theme') === 'light', () => false);
  const toggleTheme = () => { localStorage.setItem('bank-converter-theme', isLightMode ? 'dark' : 'light'); window.dispatchEvent(new Event('theme-change')); };

  return (
    <ThemeContext.Provider value={{ isLightMode, toggleTheme }}>
      <div className="theme-root" data-theme={isLightMode ? 'light' : 'dark'}>{children}</div>
    </ThemeContext.Provider>
  );
}

export function useTheme() {
  const context = useContext(ThemeContext);

  if (!context) {
    throw new Error('useTheme must be used within a ThemeProvider');
  }

  return context;
}
