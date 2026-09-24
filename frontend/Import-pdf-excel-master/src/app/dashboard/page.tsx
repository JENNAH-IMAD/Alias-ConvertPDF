 'use client';
import { AnimatedCard as Card } from '@/components/ui/card';

import Link from 'next/link';
import { Users, Landmark, Wallet, ArrowUpRight } from 'lucide-react';
import { Dashboard } from '@/services/api';
import { useApi } from '@/services/use-api';
import { useAuth } from '@/components/auth-context';
import { LoadState } from '@/components/resource-editor';
export default function DashboardPage() {
  const { data, error, loading } = useApi<Dashboard>('/dashboard');
  const { user } = useAuth();
  const metrics = [
    { label: 'Clients', value: data?.clients, icon: Users, href: '/clients', note: 'Coordonnées et informations fiscales' },
    { label: 'Banques', value: data?.banks, icon: Landmark, href: '/banks', note: 'Établissements et comptes associés' },
    { label: 'Comptes bancaires', value: data?.accounts, icon: Wallet, href: '/bank-accounts', note: 'Comptes de vos clients et paramètres comptables' },
  ];
  return <div className="ui-page"><section className="ui-hero"><p className="ui-eyebrow">TABLEAU DE BORD</p><h1 className="ui-title">Bonjour, {user?.name?.split(' ')[0] || 'bienvenue'} <span className="accent-text">.</span></h1><p className="ui-description">Une vue claire sur vos clients, leurs banques et leurs comptes.</p></section><LoadState error={error} loading={loading}/><section className="grid gap-5 md:grid-cols-3">{metrics.map(({ label, value, icon: Icon, href, note }) => <Card asChild key={label}><Link href={href} key={label} className="ui-card client-card"><div className="flex items-center justify-between"><span className="metric-icon"><Icon size={21}/></span><ArrowUpRight size={16} className="muted"/></div><p className="metric-value">{value ?? '—'}</p><h2 className="font-semibold">{label}</h2><p className="ui-description">{note}</p></Link></Card>)}</section></div>;
}
