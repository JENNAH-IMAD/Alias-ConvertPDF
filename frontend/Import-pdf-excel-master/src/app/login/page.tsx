'use client';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { FormEvent, useEffect, useState } from 'react';
import { ArrowRight, Eye, EyeOff } from 'lucide-react';
import { useAuth } from '@/components/auth-context';
import { AuthFrame } from '@/components/auth-frame';
export default function LoginPage(){
 const router=useRouter();const {login,isAuthenticated,isReady}=useAuth();const [email,setEmail]=useState('');const [password,setPassword]=useState('');const [visible,setVisible]=useState(false);const [error,setError]=useState('');const [busy,setBusy]=useState(false);
 useEffect(()=>{if(isReady&&isAuthenticated)router.replace('/dashboard');},[isAuthenticated,isReady,router]);
 const submit=async(e:FormEvent<HTMLFormElement>)=>{e.preventDefault();if(busy)return;setError('');setBusy(true);try{await login(email,password);router.push('/dashboard');}catch(e){setError((e as Error).message);}finally{setBusy(false);}};
 return <AuthFrame><p className="ui-eyebrow">BON RETOUR PARMI NOUS</p><h2 className="ui-title mt-3">Connectez-vous.</h2><p className="ui-description mb-8">Retrouvez vos clients, vos banques et vos comptes.</p><form onSubmit={submit} className="space-y-5"><fieldset disabled={busy} className="space-y-5"><label className="block"><span className="ui-label">Adresse e-mail</span><Input className="ui-input" type="email" autoComplete="username" required placeholder="vous@entreprise.com" value={email} onChange={e=>setEmail(e.target.value)}/></label><label className="block"><span className="ui-label">Mot de passe</span><span className="relative block"><Input className="ui-input pr-12" type={visible?'text':'password'} autoComplete="current-password" required placeholder="Votre mot de passe" value={password} onChange={e=>setPassword(e.target.value)}/><Button variant="ghost" type="button" className="absolute right-3 top-3 muted" aria-label={visible?'Masquer le mot de passe':'Afficher le mot de passe'} onClick={()=>setVisible(!visible)}>{visible?<EyeOff size={18}/>:<Eye size={18}/>}</Button></span></label></fieldset>{error&&<p role="alert" className="rounded-lg bg-rose-500/10 p-3 text-sm text-rose-500">{error}</p>}<Button variant="ghost" className="button-primary w-full" disabled={busy||!isReady}>{busy?'Connexion…':'Se connecter'}<ArrowRight size={17}/></Button></form><p className="muted mt-7 text-center text-xs">Pas encore de compte ? <Link className="accent-text font-semibold" href="/register">Créer un compte</Link></p></AuthFrame>;
}
