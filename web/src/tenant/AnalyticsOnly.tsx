import { Outlet } from 'react-router-dom'
import { canViewAnalytics } from './roles'
import { useTenant } from './useTenant'

export function AnalyticsOnly() {
  const context = useTenant()

  if (!canViewAnalytics(context.role)) {
    return (
      <p className="rounded border bg-white p-4 text-slate-600">
        Videos and retention data are available to Admins and Strategists.
      </p>
    )
  }

  return <Outlet context={context} />
}