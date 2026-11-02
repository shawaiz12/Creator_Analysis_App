import type { ReactNode } from 'react'
import { NavLink, Outlet, useParams } from 'react-router-dom'
import { useMe } from '../api/me'
import { AuditIcon, OverviewIcon, StrategiesIcon, TeamIcon, VideosIcon } from '../brand/icons'
import { NotFound } from '../screens/NotFound'
import type { TenantContext } from './useTenant'
import { canManageTeam, canViewAnalytics, canViewAudit } from './roles'

type NavItem = { to: string; label: string; icon: ReactNode; end?: boolean; visible: boolean }

export function TenantLayout() {
  const { tenantId = '' } = useParams()
  const me = useMe()

  if (me.isPending) return <p className="text-subtle">Loading…</p>

  if (me.isError) {
    return (
      <p className="rounded-lg border border-bad/40 bg-bad-soft p-4 text-bad">
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

  const base = `/orgs/${context.tenantId}`

  const items: NavItem[] = [
    { to: base, label: 'Overview', icon: <OverviewIcon />, end: true, visible: true },
    { to: `${base}/videos`, label: 'Videos', icon: <VideosIcon />, visible: canViewAnalytics(context.role) },
    { to: `${base}/strategies`, label: 'Strategies', icon: <StrategiesIcon />, visible: true },
    { to: `${base}/team`, label: 'Team', icon: <TeamIcon />, visible: canManageTeam(context.role) },
    { to: `${base}/audit`, label: 'Audit', icon: <AuditIcon />, visible: canViewAudit(context.role) },
  ].filter((item) => item.visible)

  const railLink = ({ isActive }: { isActive: boolean }) =>
    `flex items-center gap-3 rounded-lg px-3 py-2 text-[15px] font-medium ${
      isActive ? 'bg-raised text-ink' : 'text-muted hover:bg-raised hover:text-ink'
    }`

  const dockLink = ({ isActive }: { isActive: boolean }) =>
    `flex min-w-16 flex-1 flex-col items-center gap-1 px-2 py-2 text-[11px] font-medium ${
      isActive ? 'text-ink' : 'text-subtle'
    }`

  return (
    <div className="grid gap-8 pb-20 md:grid-cols-[13rem_1fr] md:pb-0">
      <aside className="hidden md:block">
        <div className="sticky top-20">
          <h2 className="font-display text-xl font-semibold leading-tight">{context.name}</h2>
          <p className="mt-1 text-sm text-subtle">
            You are {context.role === 'Strategist' ? 'a' : 'an'} {context.role}
          </p>

          <nav aria-label="Organization" className="mt-6 flex flex-col gap-1">
            {items.map((item) => (
              <NavLink key={item.to} to={item.to} end={item.end} className={railLink}>
                {item.icon}
                {item.label}
              </NavLink>
            ))}
          </nav>
        </div>
      </aside>

      <div className="min-w-0">
        <div className="mb-5 md:hidden">
          <h2 className="font-display text-xl font-semibold leading-tight">{context.name}</h2>
          <p className="text-sm text-subtle">Your role: {context.role}</p>
        </div>

        <Outlet context={context} />
      </div>

      <nav
        aria-label="Organization"
        className="fixed inset-x-0 bottom-0 z-20 flex border-t bg-rail/95 backdrop-blur md:hidden"
      >
        {items.map((item) => (
          <NavLink key={item.to} to={item.to} end={item.end} className={dockLink}>
            {item.icon}
            {item.label}
          </NavLink>
        ))}
      </nav>
    </div>
  )
}
