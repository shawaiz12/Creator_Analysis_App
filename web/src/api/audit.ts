import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { tenantFetch, tenantKey } from './tenant'

export const AUDIT_PAGE_SIZE = 20

export type AuditEntry = {
  id: string
  eventType: string
  recordedAtUtc: string
  // The event as it was stored. Usually an object; the API falls back to plain text if it is not JSON.
  data: unknown
}

// Page 0 is the newest. The API has no total count, so "is there an older page" is
// answered by whether this page came back full.
export function useAudit(tenantId: string, page: number, live: boolean) {
  return useQuery({
    queryKey: tenantKey(tenantId, 'audit', String(page)),
    queryFn: () =>
      tenantFetch<AuditEntry[]>(tenantId, `/audit?take=${AUDIT_PAGE_SIZE}&skip=${page * AUDIT_PAGE_SIZE}`),
    placeholderData: keepPreviousData,
    refetchInterval: live ? 5000 : false,
  })
}