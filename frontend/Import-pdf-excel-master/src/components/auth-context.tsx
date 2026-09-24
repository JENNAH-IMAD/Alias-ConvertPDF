'use client';
import { createContext, useCallback, useContext, useEffect, useState } from 'react';
import { getSession, request, Session, User } from '@/services/api';
interface AuthContextValue { isAuthenticated:boolean; user:User|null; isReady:boolean; can:(permission:string)=>boolean; login:(email:string,password:string)=>Promise<void>; logout:()=>void; }
const AuthContext = createContext<AuthContextValue | undefined>(undefined);
export function AuthProvider({children}:{children:React.ReactNode}) {
  const [user,setUser] = useState<User|null>(null); const [isReady,setReady] = useState(false);
  const logout = useCallback(()=> {sessionStorage.removeItem('bank-converter-session');setUser(null);},[]);
  useEffect(()=> {
    let mounted=true;
    const refresh=async()=>{const session=getSession();if(!session){logout();setReady(true);return;}try{const current=await request<User>('/auth/me');if(mounted&&getSession()?.token===session.token){sessionStorage.setItem('bank-converter-session',JSON.stringify({...session,user:current}));setUser(current);}}catch{if(mounted&&getSession()?.token===session.token)logout();}finally{if(mounted)setReady(true);}};
    void refresh();
    window.addEventListener('session-expired',logout);
    const timer = setInterval(()=> {void refresh();},15000);
    return ()=> {mounted=false;window.removeEventListener('session-expired',logout);clearInterval(timer);};
  },[logout]);
  const login = async (email:string,password:string) => {const session = await request<Session>('/auth/login',{method:'POST',body:JSON.stringify({email,password})});sessionStorage.setItem('bank-converter-session',JSON.stringify(session));setUser(session.user);};
  return <AuthContext.Provider value={{isAuthenticated:Boolean(user),user,isReady,can:(permission)=>user?.role==='Admin'||Boolean(user?.permissions?.includes(permission)),login,logout}}>{children}</AuthContext.Provider>;
}
export function useAuth() {const context=useContext(AuthContext);if(!context)throw new Error('AuthProvider requis');return context;}
