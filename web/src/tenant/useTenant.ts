import { useOutletContext } from 'react-router-dom'

export type TenantContext = {
  tenantId: string
  name: string
  role: string
}

// Screens inside an organization call this to learn which organization they are in.
export function useTenant() {
  return useOutletContext<TenantContext>()
}