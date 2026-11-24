import { tokenStorage } from '../auth/tokenStorage';

// Sem VITE_API_URL usa o mesmo host da página na porta 5000 (funciona também via IP da rede local).
const envUrl = (import.meta.env.VITE_API_URL as string | undefined)?.trim();
export const API_URL = (envUrl || `${window.location.protocol}//${window.location.hostname}:5000`).replace(/\/$/, '');

export class ApiError extends Error {
  status: number;
  fieldErrors: Record<string, string[]>;
  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

let onUnauthorized: (() => void) | null = null;
export const setUnauthorizedHandler = (fn: (() => void) | null) => { onUnauthorized = fn; };

type Query = Record<string, string | number | boolean | null | undefined>;

function buildUrl(path: string, query?: Query) {
  const url = new URL(API_URL + path);
  if (query) {
    for (const [k, v] of Object.entries(query)) {
      if (v !== undefined && v !== null && v !== '') url.searchParams.set(k, String(v));
    }
  }
  return url.toString();
}

function safeParse(text: string): unknown {
  try { return JSON.parse(text); } catch { return undefined; }
}

async function request<T>(method: string, path: string, body?: unknown, query?: Query): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  const token = tokenStorage.get();
  if (token) headers.Authorization = `Bearer ${token}`;
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  let res: Response;
  try {
    res = await fetch(buildUrl(path, query), {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new ApiError(0, 'Não foi possível conectar ao servidor. Verifique se a API está em execução.');
  }

  if (res.status === 401 && token && !path.startsWith('/api/auth/login')) onUnauthorized?.();
  if (res.status === 204) return undefined as T;

  const text = await res.text();
  const data = text ? safeParse(text) : undefined;

  if (!res.ok) {
    const problem = (data ?? {}) as { title?: string; errors?: Record<string, string[]> };
    const fieldErrors = problem.errors ?? {};
    const first = Object.values(fieldErrors)[0]?.[0];
    const fallback: Record<number, string> = {
      401: 'Sessão expirada ou credenciais inválidas.',
      403: 'Você não tem permissão para esta ação.',
      404: 'Conteúdo não encontrado.',
    };
    throw new ApiError(res.status, first ?? problem.title ?? fallback[res.status] ?? 'Ocorreu um erro inesperado.', fieldErrors);
  }
  return data as T;
}

/** Baixa um arquivo autenticado (CSV) e dispara o download no navegador. */
async function download(path: string, filename: string): Promise<void> {
  const token = tokenStorage.get();
  let res: Response;
  try {
    res = await fetch(buildUrl(path), { headers: token ? { Authorization: `Bearer ${token}` } : {} });
  } catch {
    throw new ApiError(0, 'Não foi possível conectar ao servidor. Verifique se a API está em execução.');
  }
  if (!res.ok) throw new ApiError(res.status, res.status === 403 ? 'Você não tem permissão para esta exportação.' : 'Não foi possível exportar.');
  const url = URL.createObjectURL(await res.blob());
  const a = document.createElement('a');
  a.href = url; a.download = filename;
  document.body.appendChild(a); a.click(); a.remove();
  URL.revokeObjectURL(url);
}

export const api = {
  download,
  get: <T>(path: string, query?: Query) => request<T>('GET', path, undefined, query),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body ?? {}),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, body ?? {}),
  patch: <T>(path: string, body?: unknown) => request<T>('PATCH', path, body ?? {}),
  delete: <T>(path: string) => request<T>('DELETE', path),
};
