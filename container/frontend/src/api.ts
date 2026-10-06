export class ApiError extends Error {
  constructor(message:string, public status:number){super(message);}
}
export async function api<T>(path:string, method='GET', body?:unknown):Promise<T> {
  const response=await fetch(`/api${path}`,{method,headers:{'Content-Type':'application/json','X-App-Request':'1'},body:body===undefined?undefined:JSON.stringify(body)});
  if(!response.ok){let message='Požadavek se nepodařilo dokončit.';try{const error=await response.json();message=error.message??error.detail??message;}catch{}throw new ApiError(message,response.status);}
  const text=await response.text();return text?JSON.parse(text) as T:undefined as T;
}
export const errorMessage=(error:unknown)=>error instanceof Error?error.message:'Něco se nepodařilo. Zkus to znovu.';
export type Me={email:string,isAdmin:boolean,localMode:boolean};
