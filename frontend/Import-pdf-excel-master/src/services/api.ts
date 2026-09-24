export const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5080/api';
export type User = { id: string; name: string; email: string; role: string; permissions?: string[] };
export type Session = { token: string; expiresAt: string; user: User };
export type Page<T> = { items: T[]; total: number; page: number; pageSize: number };
export type Client = { id: string; name: string; legalName: string; ice: string; if: string; rc: string; email: string; phone: string; country: string; banks: string[]; photoVersion: string | null; status: string };
export type Bank = { id: string; name: string; code: string; description: string; clients: number; accounts: number; logoVersion: string | null };
export type Account = { id: string; clientId: string; bankId: string; bank: string; accountNumber: string; accountName: string; currency: string; journal: string; accountCode: string };

export function getSession(): Session | null {
  if (typeof window === 'undefined') return null;
  try {
    const session = JSON.parse(sessionStorage.getItem('bank-converter-session') || 'null') as Session | null;
    return session?.token && Date.parse(session.expiresAt) > Date.now() ? session : null;
  } catch { return null; }
}
export async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers);
  const session = getSession();
  if (session) headers.set('Authorization', `Bearer ${session.token}`);
  if (options.body && !(options.body instanceof FormData)) headers.set('Content-Type', 'application/json');
  let response: Response;
  try { response = await fetch(`${API_URL}${path}`, { ...options, headers, cache: 'no-store' }); }
  catch { throw new Error('API inaccessible. Vérifiez que le backend est démarré.'); }
  if (!response.ok) {
    if (response.status === 401 && !path.startsWith('/auth/')) window.dispatchEvent(new Event('session-expired'));
    const problem = await response.json().catch(() => null);
    const errors = problem?.errors ? Object.values(problem.errors).flat().join(' ') : '';
    throw new Error(errors || problem?.title || (response.status === 403 ? 'Vous ne disposez pas de la permission nécessaire.' : `Erreur HTTP ${response.status}`));
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}
export async function all<T>(path: string): Promise<T[]> {
  const result: T[] = [];
  let page = 1;
  while (true) {
    const data = await request<Page<T>>(`${path}?page=${page}&pageSize=100`);
    result.push(...data.items);
    if (result.length >= data.total || !data.items.length) return result;
    page++;
  }
}
export type Dashboard = { clients: number; banks: number; accounts: number };
