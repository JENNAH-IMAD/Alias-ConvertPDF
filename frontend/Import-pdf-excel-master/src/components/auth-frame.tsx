'use client';
import { Button } from '@/components/ui/button';

import { Layers, Moon, Sun, Users, ArrowRight, Landmark, ShieldCheck } from 'lucide-react';
import { BlurFade } from './ui/blur-fade';
import { Brand } from './brand';
import { useTheme } from './theme-context';
export function AuthFrame({children}:{children:React.ReactNode}){
 const {isLightMode,toggleTheme}=useTheme();
 return <div className="auth-layout"><section className="auth-story"><div className="brand" data-theme="dark"><Brand compact/></div><div className="auth-story-content"><p className="ui-eyebrow">MOINS DE SAISIE. PLUS DE CLARTÉ.</p><h1>Vos clients et vos banques.<br/><span>Un travail simplifié.</span></h1><p>Centralisez vos clients, leurs banques et leurs comptes dans un seul espace.</p><div className="auth-flow"><span><Users size={30}/><small>Clients</small></span><ArrowRight size={20}/><span><Layers size={30}/><small>ALIAS</small></span><ArrowRight size={20}/><span><Landmark size={30}/><small>Banques et comptes</small></span></div><div className="flex items-center gap-2 text-xs"><ShieldCheck size={17}/> Un espace dédié à votre activité.</div></div><p className="text-xs opacity-60">ALIAS · Gestion des clients et comptes</p></section><section className="auth-form-section"><Button variant="ghost" onClick={toggleTheme} className="icon-button auth-theme" aria-label={isLightMode?'Activer le thème sombre':'Activer le thème clair'}>{isLightMode?<Moon size={19}/>:<Sun size={19}/>}</Button><div className="auth-form-content"><div className="auth-mobile-brand"><Brand/></div><BlurFade duration={0.3} blur="2px">{children}</BlurFade></div><p className="muted text-center text-xs">Votre espace de travail, à portée de main.</p></section></div>;
}
