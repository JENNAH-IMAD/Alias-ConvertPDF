'use client';
import { useCallback, useEffect, useState } from 'react';
import { request } from './api';
export function useApi<T>(path: string) {
  const [result, setResult] = useState<{ path: string; version: number; data: T | null; error: string } | null>(null);
  const [version, setVersion] = useState(0);
  const reload = useCallback(() => setVersion(v => v + 1), []);
  useEffect(() => {
    let active = true;
    request<T>(path).then(data => { if (active) setResult({path, version, data, error: ''}); }).catch(e => { if (active) setResult({path, version, data: null, error: e.message}); });
    return () => { active = false; };
  }, [path, version]);
  const current = result?.path === path && result.version === version;
  return { data: current ? result.data : null, error: current ? result.error : '', loading: !current, reload };
}
