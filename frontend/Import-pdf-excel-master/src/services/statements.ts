import { API_URL, getSession } from './api';
export type Statement = { id:string; bankAccountId:string; clientId:string; client:string; bank:string; bankId:string; account:string; originalFileName:string; currency:string; status:string; transactionCount:number; createdAt:string; archivedAt:string|null; version:string; periodStart:string|null; periodEnd:string|null };
export type Transaction = { transactionDate:string|null; valueDate:string|null; reference:string; description:string; debit:number|null; credit:number|null; balance:number|null };
export type Field = {sourceField:string; outputField:string; position:number; required:boolean; defaultValue:string};
export type Template = {id:string; name:string; code:string; type:string; encoding:string; delimiter:string; dateFormat:string; decimalSeparator:string; decimals:number; includeHeader:boolean; isActive:boolean; version:string; fields:Field[]};
export type Detail = Statement & {message:string; pageCount:number; pdfType:string; openingBalance:number|null; closingBalance:number|null; validatedAt:string|null; profileId:string|null; rawText:string; overlappingPeriod:boolean; transactions:Transaction[]; check:{totalDebit:number;totalCredit:number;calculatedBalance:number|null;difference:number|null;issues:{code:string;message:string;row:number|null}[]}; history:{id:string;action:string;status:string;message:string;createdAt:string}[]; exports:{id:string;fileName:string;fileSize:number;createdAt:string}[]};
export const statusLabel:Record<string,string>={UPLOADED:'PDF importé',QUEUED:'En attente',ANALYZING:'Analyse',OCR_PROCESSING:'Lecture OCR',UNKNOWN_FORMAT:'Format inconnu',REVIEW_REQUIRED:'À vérifier',VALIDATED:'Validé',EXPORTED:'Exporté',ARCHIVED:'Archivé',EXTRACTION_FAILED:'Échec extraction',OCR_FAILED:'Échec OCR'};
export async function download(path:string,name:string) {
 const response=await fetch(API_URL+path,{headers:{Authorization:`Bearer ${getSession()?.token??''}`}});
 if(!response.ok){if(response.status===401)window.dispatchEvent(new Event('session-expired'));throw new Error('Téléchargement impossible ou accès refusé.');}
 const url=URL.createObjectURL(await response.blob());const a=document.createElement('a');a.href=url;a.download=name;document.body.append(a);a.click();a.remove();setTimeout(()=>URL.revokeObjectURL(url),10000);
}
