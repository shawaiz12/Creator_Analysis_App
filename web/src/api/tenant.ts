import { apiFetch } from './apiFetch'

// Every query about an organization starts with ['tenant', tenantId, ...].
export const tenantKey = (tenantId: string, ...parts: string[]) => ['tenant', tenantId, ...parts] as const

// Calls /api/{tenantId}/... with the signed-in user's token.
export function tenantFetch<T>(tenantId: string, path: string, init?: RequestInit) {
  return apiFetch<T>(`/api/${tenantId}${path}`, init)
}