'use client';
import { AnimatedCard as Card } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';

import { Sun, Moon, UserRound, ShieldCheck } from 'lucide-react';
import { useTheme } from '@/components/theme-context';
import { useAuth } from '@/components/auth-context';
import { Avatar } from '@/components/avatar';
export default function SettingsPage(){
 const {isLightMode,toggleTheme}=useTheme();const {user}=useAuth();
 return <div className="ui-page"><section className="ui-hero"><p className="ui-eyebrow">VOTRE ESPACE</p><h1 className="ui-title">Profil et préférences</h1><p className="ui-description">Retrouvez vos informations et personnalisez votre interface.</p></section><div className="grid gap-5 lg:grid-cols-2"><Card asChild><section className="ui-card"><div className="mb-6 flex items-center gap-2 font-semibold"><UserRound size={18} className="accent-text"/> Mon profil</div><div className="mb-6 flex items-center gap-4"><Avatar name={user?.name||'Utilisateur'}/><div><h2 className="text-lg font-bold">{user?.name}</h2><p className="muted mt-1 break-all text-xs">{user?.email}</p></div></div><div className="ui-row"><span className="muted">Rôle</span><Badge variant="outline" className="ui-tag"><ShieldCheck size={13}/>{user?.role==='Admin'?'Administrateur':'Utilisateur'}</Badge></div></section></Card><Card asChild><section className="ui-card"><h2 className="font-semibold">Apparence</h2><p className="ui-description">Le thème est mémorisé sur ce navigateur.</p><div className="mt-6 grid grid-cols-2 gap-4">{[{light:true,label:'Clair',icon:Sun},{light:false,label:'Sombre',icon:Moon}].map(({light,label,icon:Icon})=><Button variant="ghost" key={label} aria-pressed={isLightMode===light} onClick={()=>{if(isLightMode!==light)toggleTheme();}} className={`theme-choice ${isLightMode===light?'selected':''}`}><span className={`theme-preview ${light?'preview-light':'preview-dark'}`} aria-hidden="true"><span/><span><i/><i/><i/></span></span><span className="mt-3 flex items-center gap-2 text-xs font-semibold"><Icon size={15}/>{label}{isLightMode===light&&<span className="accent-text ml-auto">Actif</span>}</span></Button>)}</div></section></Card></div></div>;
}
