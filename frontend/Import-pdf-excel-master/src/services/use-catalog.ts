'use client';
import { useEffect, useState } from 'react';
import { all } from './api';
/** Load catalog selectors across every server page, without a silent 100-item cap. */
export function useCatalog<T>(path:string){
 const [result,setResult]=useState<{path:string;items:T[];error:string}|null>(null);
 useEffect(()=>{let active=true;all<T>(path).then(items=>{if(active)setResult({path,items,error:''});}).catch(e=>{if(active)setResult({path,items:[],error:(e as Error).message});});return()=>{active=false;};},[path]);
 return {data:result?.path===path?{items:result.items}:null,error:result?.path===path?result.error:'',loading:result?.path!==path};
}
