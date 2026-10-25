import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from './apiFetch'

export type Membership = {
  organizationId: string
  organizationName: string
  role: string
}

export type Me = {
  email: string | null
  organizations: Membership[]
}

export const meKey = ['me'] as const

export function useMe() {
  return useQuery({ queryKey: meKey, queryFn: () => apiFetch<Me>('/api/me') })
}

export function useCreateOrganization() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (name: string) =>
      apiFetch<{ organizationId: string }>('/api/onboarding/organizations', {
        method: 'POST',
        body: JSON.stringify({ name }),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: meKey }),
  })
}