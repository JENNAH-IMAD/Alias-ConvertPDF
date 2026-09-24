'use client';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { AnimatedCard as Card } from '@/components/ui/card';


import { FormEvent, useEffect, useState } from 'react';
import { Plus, Pencil, Trash2, KeyRound, LogOut, ShieldCheck } from 'lucide-react';
import { Dialog } from '@/components/dialog';
import { Avatar } from '@/components/avatar';
import { LoadState, Pagination } from '@/components/resource-editor';
import { useAuth } from '@/components/auth-context';
import { useApi } from '@/services/use-api';
import { Page, request } from '@/services/api';

type ManagedUser = { id: string; name: string; email: string; role: string; isActive: boolean; permissions: string[]; createdAt: string; updatedAt: string };
const modules = [{key:'dashboard',label:'Tableau de bord'}, {key:'clients',label:'Clients'}, {key:'banks',label:'Banques'}, {key:'accounts',label:'Comptes bancaires'}];
const actions = [{key:'read',label:'Consulter'}, {key:'write',label:'Créer / modifier'}, {key:'delete',label:'Supprimer'}];

function UserEditor({initial, onClose, onSaved}:{initial:ManagedUser|null;onClose:()=>void;onSaved:()=>void}) {
  const {user}=useAuth();
  const {data:catalog,error:catalogError,loading}=useApi<{all:string[];defaults:string[]}>('/users/permissions');
  const [name,setName]=useState(initial?.name||''); const [email,setEmail]=useState(initial?.email||'');
  const [role,setRole]=useState(initial?.role||'User');const [active,setActive]=useState(initial?.isActive??true);
  const [permissions,setPermissions]=useState<string[]|null>(initial?.permissions??null);
  const [password,setPassword]=useState('');const [passwordConfirmation,setPasswordConfirmation]=useState('');const [error,setError]=useState('');const [busy,setBusy]=useState(false);
  const selected=permissions??catalog?.defaults??[];
  const self=initial?.id===user?.id;
  function toggle(permission:string,checked:boolean){
    const next=new Set(selected);
    if(checked){next.add(permission);const area=permission.split('.')[0];next.add(area+'.read');if(permission==='accounts.write'){next.add('clients.read');next.add('banks.read');}if(permission==='clients.delete'||permission==='banks.delete'){next.add('accounts.read');next.add('accounts.delete');}}
    else {next.delete(permission);if(permission.endsWith('.read')){const area=permission.split('.')[0];next.delete(area+'.write');next.delete(area+'.delete');}if(permission==='clients.read'||permission==='banks.read')next.delete('accounts.write');if(permission==='accounts.delete'||permission==='accounts.read'){next.delete('clients.delete');next.delete('banks.delete');}}
    setPermissions([...next]);
  }
  async function save(event:FormEvent){event.preventDefault();if(busy||!catalog)return;if(!initial&&password!==passwordConfirmation){setError('Les deux mots de passe doivent être identiques.');return;}setBusy(true);setError('');try{await request('/users'+(initial?'/'+initial.id:''),{method:initial?'PUT':'POST',body:JSON.stringify({name,email,role,isActive:active,permissions:role==='Admin'?catalog.all:selected,password:initial?null:password,version:initial?.updatedAt??null})});onSaved();onClose();}catch(e){setError((e as Error).message);}finally{setBusy(false);}}
  return <Dialog title={initial?'Modifier l’utilisateur':'Créer un utilisateur'} onClose={onClose} busy={busy} wide>
    <form onSubmit={save} className="space-y-5"><fieldset disabled={busy||loading} className="space-y-5">
      <div className="grid gap-4 sm:grid-cols-2"><label>Nom<Input className="ui-input mt-2" required maxLength={100} value={name} onChange={e=>setName(e.target.value)}/></label><label>E-mail<Input className="ui-input mt-2" required type="email" maxLength={254} value={email} onChange={e=>setEmail(e.target.value)}/></label>
      <label>Rôle<select className="ui-input mt-2" value={role} disabled={self} onChange={e=>{setRole(e.target.value);if(e.target.value==='User')setPermissions(catalog?.defaults??[]);}}><option value="User">Utilisateur</option><option value="Admin">Administrateur</option></select></label>
      <label>État<select className="ui-input mt-2" disabled={self} value={String(active)} onChange={e=>setActive(e.target.value==='true')}><option value="true">Actif</option><option value="false">Désactivé</option></select></label></div>
      {!initial&&<label className="block">Mot de passe initial<Input className="ui-input mt-2" type="password" autoComplete="new-password" required minLength={12} maxLength={128} value={password} onChange={e=>setPassword(e.target.value)}/><small className="muted">12 à 128 caractères, majuscule, minuscule et chiffre.</small></label>}
      {!initial&&<label className="block">Confirmer le mot de passe<Input className="ui-input mt-2" type="password" autoComplete="new-password" required maxLength={128} value={passwordConfirmation} onChange={e=>setPasswordConfirmation(e.target.value)}/></label>}
      {self&&<p className="ui-description">Votre propre compte ne peut pas être désactivé ou rétrogradé.</p>}
      <section className="ui-summary"><h3 className="font-semibold mb-3">Permissions par fonctionnalité</h3><p className="ui-description mb-4">{role==='Admin'?'Les administrateurs disposent de tous les droits, y compris la gestion des utilisateurs.':'Les dépendances nécessaires sont sélectionnées automatiquement. Supprimer un client ou une banque peut supprimer ses comptes associés.'}</p>
      <div className="overflow-x-auto"><table className="w-full text-sm"><thead><tr><th className="text-left p-2">Fonctionnalité</th>{actions.map(a=><th className="p-2" key={a.key}>{a.label}</th>)}</tr></thead><tbody>{modules.map(m=><tr key={m.key}><th className="text-left p-2">{m.label}</th>{actions.map(a=><td className="text-center p-3" key={a.key}>{m.key==='dashboard'&&a.key!=='read'?'—':<input type="checkbox" aria-label={`${m.label} : ${a.label}`} disabled={role==='Admin'} checked={role==='Admin'||selected.includes(m.key+'.'+a.key)} onChange={e=>toggle(m.key+'.'+a.key,e.target.checked)}/>}</td>)}</tr>)}</tbody></table></div></section>
      {initial&&<p className="ui-description">L’enregistrement déconnecte les sessions existantes de cet utilisateur.{self?' Vous devrez vous reconnecter.':''}</p>}
    </fieldset><LoadState error={error||catalogError} loading={loading}/><div className="flex justify-end gap-3"><Button variant="ghost" type="button" className="button-secondary" disabled={busy} onClick={onClose}>Annuler</Button><Button variant="ghost" className="button-primary" disabled={busy||loading||!catalog}>{busy?'Enregistrement…':'Enregistrer'}</Button></div></form>
  </Dialog>;
}

export default function UsersPage(){
  const {user}=useAuth();const [page,setPage]=useState(1);const [search,setSearch]=useState('');const [role,setRole]=useState('');const [active,setActive]=useState('');
  const [term,setTerm]=useState('');
  useEffect(()=>{const timer=setTimeout(()=>{setTerm(search);setPage(1);},300);return()=>clearTimeout(timer);},[search]);
  const {data,error,loading,reload}=useApi<Page<ManagedUser>>(`/users?page=${page}&pageSize=20&search=${encodeURIComponent(term)}&role=${role}${active?'&active='+active:''}`);
  const [editing,setEditing]=useState<ManagedUser|null|undefined>(undefined);
  const [action,setAction]=useState<{kind:'delete'|'password'|'revoke';user:ManagedUser}|null>(null);
  const [password,setPassword]=useState('');const [passwordConfirmation,setPasswordConfirmation]=useState('');const [confirmed,setConfirmed]=useState(false);const [busy,setBusy]=useState(false);const [actionError,setActionError]=useState('');const [notice,setNotice]=useState('');
  function open(kind:'delete'|'password'|'revoke',target:ManagedUser){setAction({kind,user:target});setPassword('');setPasswordConfirmation('');setConfirmed(false);setActionError('');}
  async function execute(e:FormEvent){e.preventDefault();if(!action||busy)return;if(action.kind==='password'&&password!==passwordConfirmation){setActionError('Les deux mots de passe doivent être identiques.');return;}if(action.kind!=='password'&&!confirmed)return;setBusy(true);setActionError('');try{const path='/users/'+action.user.id;await request(path+(action.kind==='password'?'/password':action.kind==='revoke'?'/revoke-sessions':''),{method:action.kind==='delete'?'DELETE':action.kind==='password'?'PUT':'POST',...(action.kind==='password'?{body:JSON.stringify({password})}:{})});setAction(null);setNotice('Opération effectuée.');if(action.kind==='delete'&&data?.items.length===1&&page>1)setPage(page-1);else reload();}catch(e){setActionError((e as Error).message);}finally{setBusy(false);}}
  return <div className="ui-page"><section className="ui-hero flex flex-wrap justify-between items-center gap-4"><div><p className="ui-eyebrow">ADMINISTRATION</p><h1 className="ui-title">Utilisateurs</h1><p className="ui-description">Gérez les comptes, les rôles, les accès et les sessions.</p></div><Button variant="ghost" className="button-primary" onClick={()=>setEditing(null)}><Plus size={17}/>Nouvel utilisateur</Button></section>
    <Card asChild><section className="ui-card flex flex-wrap gap-4"><label className="flex-1">Rechercher<Input className="ui-input mt-2" type="search" maxLength={200} placeholder="Nom ou e-mail" value={search} onChange={e=>{setSearch(e.target.value);setPage(1);}}/></label><label>Rôle<select className="ui-input mt-2" value={role} onChange={e=>{setRole(e.target.value);setPage(1);}}><option value="">Tous</option><option value="Admin">Administrateur</option><option value="User">Utilisateur</option></select></label><label>État<select className="ui-input mt-2" value={active} onChange={e=>{setActive(e.target.value);setPage(1);}}><option value="">Tous</option><option value="true">Actif</option><option value="false">Désactivé</option></select></label></section></Card>
    <div className="flex flex-wrap items-center justify-between gap-3"><span className="muted text-sm">{data?.total??'—'} utilisateur(s)</span><div className="flex gap-2">{(search||role||active)&&<Button variant="ghost" className="button-secondary" onClick={()=>{setSearch('');setTerm('');setRole('');setActive('');setPage(1);}}>Réinitialiser les filtres</Button>}<Button variant="ghost" className="button-secondary" disabled={loading} onClick={reload}>Actualiser</Button></div></div>
    {notice&&<p role="status" className="status-positive">{notice}</p>}<LoadState error={error} loading={loading} empty={!data?.items.length}/>
    <section className="grid gap-5 lg:grid-cols-2">{data?.items.map(item=><Card asChild key={item.id}><article className="ui-card" key={item.id}><div className="flex items-center gap-3"><Avatar name={item.name}/><div className="min-w-0 flex-1"><h2 className="font-bold break-words">{item.name}{item.id===user?.id?' (vous)':''}</h2><p className="muted text-sm break-all">{item.email}</p></div><span className={item.isActive?'status-positive':'ui-tag'}>{item.isActive?'Actif':'Désactivé'}</span></div><p className="mt-4 flex gap-2 items-center"><ShieldCheck size={16}/>{item.role==='Admin'?'Administrateur · tous les droits':`Utilisateur · ${item.permissions.length} permissions`}</p><p className="muted text-xs mt-2">Créé le {new Date(item.createdAt).toLocaleDateString('fr-FR')}</p><div className="flex flex-wrap gap-2 mt-5"><Button variant="ghost" className="button-secondary" onClick={()=>setEditing(item)}><Pencil size={14}/>Modifier et permissions</Button><Button variant="ghost" className="button-secondary" onClick={()=>open('password',item)}><KeyRound size={14}/>Mot de passe</Button><Button variant="ghost" className="button-secondary" onClick={()=>open('revoke',item)}><LogOut size={14}/>Déconnecter</Button><Button variant="ghost" className="button-secondary danger-button" disabled={item.id===user?.id} onClick={()=>open('delete',item)}><Trash2 size={14}/>Supprimer</Button></div></article></Card>)}</section>
    <Pagination page={page} total={data?.total||0} onChange={setPage}/>
    {editing!==undefined&&<UserEditor initial={editing} onClose={()=>setEditing(undefined)} onSaved={()=>{setNotice('Utilisateur enregistré.');reload();}}/>}
    {action&&<Dialog title={action.kind==='delete'?'Supprimer l’utilisateur':action.kind==='password'?'Réinitialiser le mot de passe':'Révoquer les sessions'} onClose={()=>setAction(null)} busy={busy}><form onSubmit={execute} className="space-y-5"><p className="font-semibold">{action.user.name} · {action.user.email}</p><p className="ui-description">{action.kind==='delete'?'Le compte sera supprimé définitivement. Les clients, banques et comptes bancaires sont conservés.':'Les sessions existantes seront invalidées dès la prochaine requête.'}</p>{action.kind==='password'?<label className="block">Nouveau mot de passe<Input type="password" className="ui-input mt-2" required minLength={12} maxLength={128} autoComplete="new-password" value={password} onChange={e=>setPassword(e.target.value)}/><small>12 caractères minimum, majuscule, minuscule et chiffre.</small></label>:<label className="flex gap-2"><input required type="checkbox" checked={confirmed} onChange={e=>setConfirmed(e.target.checked)}/>Je confirme cette action.</label>}{action.kind==="password"&&<label className="block">Confirmer le mot de passe<Input type="password" className="ui-input mt-2" required maxLength={128} autoComplete="new-password" value={passwordConfirmation} onChange={e=>setPasswordConfirmation(e.target.value)}/></label>}{actionError&&<p role="alert" className="danger-button">{actionError}</p>}<Button variant="ghost" className="button-primary" disabled={busy}>{busy?'Traitement…':'Confirmer'}</Button></form></Dialog>}
  </div>;
}
