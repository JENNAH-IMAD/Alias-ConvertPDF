'use client';
import { AnimatedCard as Card } from '@/components/ui/card';


import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useEffect, useRef } from 'react';
import { Button } from './ui/button';
import { Tooltip, TooltipTrigger, TooltipContent, TooltipProvider } from './ui/tooltip';
import { MotionConfig } from 'motion/react';
import { LayoutDashboard, Users, Landmark, Wallet, Settings, Moon, Sun, Menu, X, LogOut, ChevronRight, FileText, Archive, FileOutput, FolderOpen } from 'lucide-react';
import { useAuth } from './auth-context';
import { useTheme } from './theme-context';
import { Avatar } from './avatar';
import { Brand } from './brand';
import { Sidebar, DesktopSidebar, SidebarTrigger } from './ui/sidebar';
import { BlurFade } from './ui/blur-fade';
import { useCompactSidebar } from './use-compact-sidebar';

const navigationGroups = [
  { name: 'RELEVÉS ET ARCHIVES', items: [{ href: '/conversions', label: 'Conversions', icon: FileText }, { href: '/client-space', label: 'Espace clients', icon: FolderOpen }, { href: '/archives', label: 'Archives', icon: Archive }, { href: '/templates', label: 'Modèles d’export', icon: FileOutput }] },
  { name: 'ESPACE DE TRAVAIL', items: [{ href: '/dashboard', label: 'Vue d’ensemble', icon: LayoutDashboard }] },
  { name: 'RÉFÉRENTIELS', items: [{ href: '/clients', label: 'Clients', icon: Users }, { href: '/banks', label: 'Banques', icon: Landmark }, { href: '/bank-accounts', label: 'Comptes bancaires', icon: Wallet }] },
  { name: 'CONFIGURATION', items: [{ href: '/users', label: 'Utilisateurs', icon: Users }, { href: '/settings', label: 'Paramètres', icon: Settings }] },
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { isAuthenticated, isReady, user, logout, can } = useAuth();
  const { isLightMode, toggleTheme } = useTheme();
  const { compact, toggle } = useCompactSidebar();
  const drawer = useRef<HTMLDialogElement>(null);
  const authRoute = pathname === '/login' || pathname === '/register';
  useEffect(() => {
    if (!isReady) return;
    if (!isAuthenticated && !authRoute) router.replace('/login');
    else if (isAuthenticated && authRoute) router.replace('/dashboard');
  }, [isReady, isAuthenticated, authRoute, router]);

  const access: Record<string, string> = { '/conversions': 'statements.read', '/client-space': 'statements.read', '/archives': 'statements.read', '/templates': 'statements.read', '/dashboard': 'dashboard.read', '/clients': 'clients.read', '/banks': 'banks.read', '/bank-accounts': 'accounts.read' };
  const permitted = (href: string) => href === '/users' ? user?.role === 'Admin' : href.startsWith('/statements/') ? can('statements.read') : !access[href] || can(access[href]);
  const groups = navigationGroups.map(g => ({ ...g, items: g.items.filter(i => permitted(i.href)) })).filter(g => g.items.length);
  const active = groups.flatMap(g => g.items).find(i => i.href === pathname);
  const closeDrawer = () => drawer.current?.close();
  const signOut = () => { closeDrawer(); logout(); router.replace('/login'); };

  function navigation(mini: boolean) {
    return <>
      <Link href={groups[0]?.items[0]?.href || '/settings'} className="brand sidebar-brand" aria-label="ALIAS — Accueil" onClick={closeDrawer}><Brand compact={mini}/></Link>
      <nav aria-label="Navigation principale" className="sidebar-nav">{groups.map(group => <div key={group.name} className="nav-group"><p className={`nav-caption ${mini ? 'sr-only' : ''}`}>{group.name}</p>{group.items.map(({ href, label, icon: Icon }) => {
        const link = <Link href={href} aria-label={label} aria-current={pathname === href ? 'page' : undefined} className={`nav-link ${pathname === href ? 'active' : ''}`} onClick={closeDrawer}><Icon size={20} strokeWidth={1.7}/>{!mini && <><span className="nav-label">{label}</span>{pathname === href && <ChevronRight size={14} className="ml-auto"/>}</>}</Link>;
        return mini ? <Tooltip key={href}><TooltipTrigger asChild>{link}</TooltipTrigger><TooltipContent side="right" className="navigation-tooltip">{label}</TooltipContent></Tooltip> : <span key={href} className="nav-item">{link}</span>;
      })}</div>)}</nav>
      <div className="sidebar-bottom">
        {!mini && <div className="workspace-note"><Landmark size={20}/><div><strong>Votre espace, simplifié.</strong><p>Clients, banques et comptes.</p></div></div>}
        <Link href="/settings" className="sidebar-profile" aria-label="Mon profil et mes paramètres" title={mini ? user?.name : undefined} onClick={closeDrawer}><Avatar name={user?.name || 'Utilisateur'} small={mini}/>{!mini && <><span className="min-w-0 flex-1"><strong className="block truncate">{user?.name}</strong><small className="muted">{user?.role === 'Admin' ? 'Administrateur' : 'Utilisateur'}</small></span><Settings size={17}/></>}</Link>
        <Button variant="ghost" size={mini ? 'icon' : 'default'} className="logout-button" aria-label="Se déconnecter" onClick={signOut}><LogOut size={17}/>{!mini && 'Se déconnecter'}</Button>
      </div>
    </>;
  }

  if (authRoute) return <MotionConfig reducedMotion="user">{children}</MotionConfig>;
  if (!isReady || !isAuthenticated) return <div className="app-loading" role="status"><span className="loading-dot"/>Chargement de votre espace…</div>;

  return <MotionConfig reducedMotion="user"><TooltipProvider delayDuration={100}><Sidebar open={!compact} onToggle={toggle}><div className={`app-shell ${compact ? 'sidebar-compact' : ''}`}>
    <a href="#main-content" className="skip-link">Aller au contenu</a>
    <DesktopSidebar>{navigation(compact)}</DesktopSidebar>
    <dialog ref={drawer} aria-label="Menu de navigation" className="mobile-drawer"><Button size="icon" variant="ghost" className="icon-button drawer-close" aria-label="Fermer le menu" onClick={closeDrawer}><X size={20}/></Button>{navigation(false)}</dialog>
    <div className="app-body">
      <header className="topbar"><div className="flex min-w-0 items-center gap-3">
        <SidebarTrigger/>
        <Button size="icon" variant="ghost" className="icon-button mobile-menu" aria-label="Ouvrir le menu" onClick={() => drawer.current?.showModal()}><Menu size={21}/></Button><span className="breadcrumb-root">Espace de travail</span><ChevronRight size={14} className="breadcrumb-root"/><span className="truncate font-medium">{active?.label || 'ALIAS'}</span>
      </div><div className="flex shrink-0 items-center gap-3"><Button size="icon" variant="ghost" className="icon-button theme-toggle" onClick={toggleTheme} aria-label={isLightMode ? 'Activer le thème sombre' : 'Activer le thème clair'}>{isLightMode ? <Moon size={19}/> : <Sun size={19}/>}</Button><Link href="/settings" aria-label="Mon profil"><Avatar name={user?.name || 'Utilisateur'} small/></Link></div></header>
      <main id="main-content" className="main-content"><BlurFade key={pathname} duration={0.3} blur="3px" offset={5}>{permitted(pathname) ? children : <Card asChild><div className="ui-card"><h1 className="ui-title">Accès non autorisé</h1><p className="ui-description">Demandez à un administrateur de modifier vos permissions.</p></div></Card>}</BlurFade></main>
      <footer className="app-footer"><span>ALIAS <span className="footer-divider">/</span> Gestion des clients et comptes</span><Link href="/settings">Mon espace <ChevronRight size={13}/></Link></footer>
    </div>
  </div></Sidebar></TooltipProvider></MotionConfig>;
}

