import { NavLink, Outlet, useParams } from 'react-router-dom'
import { useMe } from '../api/me'
import { NotFound } from '../screens/NotFound'
import type { TenantContext } from './useTenant'
import { canManageTeam, canViewAnalytics } from './roles'

export function TenantLayout() {
  const { tenantId = '' } = useParams()
  const me = useMe()

  if (me.isPending) return <p className="text-slate-500">Loading…</p>

  if (me.isError) {
    return (
      <p className="rounded border border-red-300 bg-red-50 p-4 text-red-800">
        Could not load your profile: {me.error.message}
      </p>
    )
  }

  const membership = me.data.organizations.find(
    (organization) => organization.organizationId.toLowerCase() === tenantId.toLowerCase(),
  )

  // Same behavior as the API: "not a member" and "does not exist" look identical.
  if (!membership) return <NotFound />

  const context: TenantContext = {
    tenantId: membership.organizationId,
    name: membership.organizationName,
    role: membership.role,
  }

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    `rounded px-3 py-1 text-sm ${isActive ? 'bg-indigo-600 text-white' : 'hover:bg-slate-100'}`

  return (
    <section>
      <h2 className="text-xl font-semibold">{context.name}</h2>
      <p className="text-sm text-slate-500">Your role: {context.role}</p>

      <nav className="mt-4 flex gap-2 border-b pb-3">
        <NavLink to={`/orgs/${context.tenantId}`} end className={linkClass}>
          Overview
        </NavLink>
        {canViewAnalytics(context.role) && (
          <NavLink to={`/orgs/${context.tenantId}/videos`} className={linkClass}>
            Videos
          </NavLink>
        )}
        <NavLink to={`/orgs/${context.tenantId}/strategies`} className={linkClass}>
          Strategies
        </NavLink>
        {canManageTeam(context.role) && (
          <NavLink to={`/orgs/${context.tenantId}/team`} className={linkClass}>
            Team
          </NavLink>
        )}
      </nav>

      <div className="mt-6">
        <Outlet context={context} />
      </div>
    </section>
  )
}