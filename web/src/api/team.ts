import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from './apiFetch'
import { meKey } from './me'
import { tenantFetch, tenantKey } from './tenant'

export type Invitation = {
  id: string
  email: string
  role: string
  status: 'Pending' | 'Accepted' | 'Revoked' | 'Expired'
  createdAtUtc: string
  expiresAtUtc: string
}

const invitationsKey = (tenantId: string) => tenantKey(tenantId, 'invitations')

export function useInvitations(tenantId: string) {
  return useQuery({
    queryKey: invitationsKey(tenantId),
    queryFn: () => tenantFetch<Invitation[]>(tenantId, '/invitations'),
  })
}

export function useCreateInvitation(tenantId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: { email: string; role: string }) =>
      tenantFetch<{ invitationId: string }>(tenantId, '/invitations', {
        method: 'POST',
        body: JSON.stringify(input),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: invitationsKey(tenantId) }),
  })
}

export function useRevokeInvitation(tenantId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (invitationId: string) =>
      tenantFetch<{ status: string }>(tenantId, `/invitations/${invitationId}/revoke`, { method: 'POST' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: invitationsKey(tenantId) }),
  })
}

// Accepting happens outside any organization: the person is not a member yet.
export function useAcceptInvitation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (invitationId: string) =>
      apiFetch<{ organizationId: string }>(`/api/invitations/${invitationId}/accept`, { method: 'POST' }),
    // Refresh "who am I" first, so the new organization exists in the cache before we navigate into it.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: meKey }),
  })
}