import { useEffect, useRef } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { tenantFetch, tenantKey } from './tenant'

export type StrategyStatus = 'Draft' | 'PendingApproval' | 'Approved' | 'NeedsRevision' | 'Implemented'

export type StrategySummary = {
  id: string
  videoId: string
  status: StrategyStatus
  createdAtUtc: string
  revisionCount: number
}

export type Revision = {
  id: string
  versionNumber: number
  content: string
  origin: 'Ai' | 'Human'
  authorUserId: string | null
  createdAtUtc: string
}

export type Review = {
  id: string
  revisionId: string
  reviewerUserId: string
  decision: 'Approved' | 'Rejected'
  reason: string | null
  reviewedAtUtc: string
}

export type Strategy = {
  id: string
  videoId: string
  status: StrategyStatus
  createdAtUtc: string
  currentRevision: Revision | null
  reviews: Review[]
}

export type GenerationRequest = {
  id: string
  status: 'Pending' | 'Succeeded' | 'Failed'
  attempts: number
  requestedAtUtc: string
  completedAtUtc: string | null
  error: string | null
  model: string | null
  promptVersion: string | null
}

// Everything about strategies lives under ['tenant', tenantId, 'strategies', ...],
// so one invalidation refreshes the list, the detail and the generation status together.
const strategiesKey = (tenantId: string) => tenantKey(tenantId, 'strategies')

export function useStrategies(tenantId: string) {
  return useQuery({
    queryKey: strategiesKey(tenantId),
    queryFn: () => tenantFetch<StrategySummary[]>(tenantId, '/strategies'),
  })
}

export function useStrategy(tenantId: string, id: string) {
  return useQuery({
    queryKey: tenantKey(tenantId, 'strategies', id),
    queryFn: () => tenantFetch<Strategy>(tenantId, `/strategies/${id}`),
  })
}

// Polls every 3 seconds while a draft request is still pending, then stops by itself.
export function useGeneration(tenantId: string, id: string, enabled: boolean) {
  return useQuery({
    queryKey: tenantKey(tenantId, 'strategies', id, 'generation'),
    queryFn: () => tenantFetch<GenerationRequest[]>(tenantId, `/strategies/${id}/generation`),
    enabled,
    refetchInterval: (query) => (query.state.data?.some((r) => r.status === 'Pending') ? 3000 : false),
  })
}

// When the last pending request finishes, the worker has just written a new revision.
// Reload the strategy so it appears without a manual refresh.
export function useRefreshWhenGenerationEnds(tenantId: string, id: string, pending: boolean) {
  const queryClient = useQueryClient()
  const wasPending = useRef(false)

  useEffect(() => {
    if (wasPending.current && !pending) {
      queryClient.invalidateQueries({ queryKey: tenantKey(tenantId, 'strategies', id) })
    }
    wasPending.current = pending
  }, [pending, queryClient, tenantId, id])
}

function useRefresh(tenantId: string) {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: strategiesKey(tenantId) })
}

export function useCreateStrategy(tenantId: string) {
  const refresh = useRefresh(tenantId)

  return useMutation({
    mutationFn: (videoId: string) =>
      tenantFetch<{ documentId: string }>(tenantId, '/strategies', {
        method: 'POST',
        body: JSON.stringify({ videoId }),
      }),
    onSuccess: refresh,
  })
}

export function useAddRevision(tenantId: string, id: string) {
  const refresh = useRefresh(tenantId)

  return useMutation({
    mutationFn: (content: string) =>
      tenantFetch<Revision>(tenantId, `/strategies/${id}/revisions`, {
        method: 'POST',
        body: JSON.stringify({ content }),
      }),
    onSuccess: refresh,
  })
}

export function useSubmit(tenantId: string, id: string) {
  const refresh = useRefresh(tenantId)

  return useMutation({
    mutationFn: () => tenantFetch<{ status: string }>(tenantId, `/strategies/${id}/submit`, { method: 'POST' }),
    onSuccess: refresh,
  })
}

export function useGenerate(tenantId: string, id: string) {
  const refresh = useRefresh(tenantId)

  return useMutation({
    mutationFn: () => tenantFetch<{ requestId: string }>(tenantId, `/strategies/${id}/generate`, { method: 'POST' }),
    onSuccess: refresh,
  })
}

export function useApprove(tenantId: string, id: string) {
  const refresh = useRefresh(tenantId)

  return useMutation({
    mutationFn: (revisionId: string) =>
      tenantFetch<{ status: string }>(tenantId, `/strategies/${id}/approve`, {
        method: 'POST',
        body: JSON.stringify({ revisionId }),
      }),
    onSuccess: refresh,
    // Even on a refused approval (for example a stale revision), reload so the screen shows the truth.
    onError: refresh,
  })
}

export function useReject(tenantId: string, id: string) {
  const refresh = useRefresh(tenantId)

  return useMutation({
    mutationFn: (input: { revisionId: string; reason: string }) =>
      tenantFetch<{ status: string }>(tenantId, `/strategies/${id}/reject`, {
        method: 'POST',
        body: JSON.stringify(input),
      }),
    onSuccess: refresh,
    onError: refresh,
  })
}

export function useMarkImplemented(tenantId: string, id: string) {
  const refresh = useRefresh(tenantId)

  return useMutation({
    mutationFn: () => tenantFetch<{ status: string }>(tenantId, `/strategies/${id}/implemented`, { method: 'POST' }),
    onSuccess: refresh,
  })
}