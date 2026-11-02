import { Link } from 'react-router-dom'
import { useVideos } from '../api/integration'
import { useStrategies, type StrategyStatus } from '../api/strategies'
import { canEditStrategy, canMarkImplemented, canReview, canViewAnalytics } from './roles'
import { StatusBadge } from './StatusBadge'
import { useTenant } from './useTenant'

// The pipeline, left to right, in the order a strategy moves through it.
const berths: { status: StrategyStatus; label: string; dot: string }[] = [
  { status: 'Draft', label: 'Drafting', dot: 'bg-subtle' },
  { status: 'PendingApproval', label: 'Awaiting approval', dot: 'bg-pending' },
  { status: 'NeedsRevision', label: 'Sent back', dot: 'bg-bad' },
  { status: 'Approved', label: 'Approved', dot: 'bg-ok' },
  { status: 'Implemented', label: 'Done', dot: 'bg-link' },
]

export function Overview() {
  const { tenantId, name, role } = useTenant()
  const strategies = useStrategies(tenantId)
  const videos = useVideos(tenantId, canViewAnalytics(role))

  const titleOf = (videoId: string) =>
    videos.data?.find((video) => video.id.toLowerCase() === videoId.toLowerCase())?.title ??
    `Video ${videoId.slice(0, 8)}`

  const all = strategies.data ?? []
  const countOf = (status: StrategyStatus) => all.filter((strategy) => strategy.status === status).length

  // What needs this person, based on what their role is allowed to do.
  const waitingOnYou = all.filter((strategy) => {
    if (strategy.status === 'PendingApproval') return canReview(role)
    if (strategy.status === 'NeedsRevision' || strategy.status === 'Draft') return canEditStrategy(role)
    if (strategy.status === 'Approved') return canMarkImplemented(role)
    return false
  })

  const actionFor = (status: StrategyStatus) => {
    if (status === 'PendingApproval') return 'Review'
    if (status === 'NeedsRevision') return 'Revise'
    if (status === 'Approved') return 'Apply'
    return 'Continue'
  }

  return (
    <div>
      <h2 className="font-display text-2xl font-semibold">Overview</h2>
      <p className="mt-1 text-muted">Where every strategy for {name} stands right now.</p>

      {strategies.isPending && <p className="mt-6 text-subtle">Loading…</p>}
      {strategies.isError && <p className="mt-6 text-bad">{strategies.error.message}</p>}

      {strategies.data && (
        <>
          <dl className="mt-6 grid grid-cols-2 overflow-hidden rounded-xl border sm:grid-cols-5">
            {berths.map((berth) => (
              <div key={berth.status} className="border-b border-r p-4 last:border-r-0 sm:border-b-0">
                <dt className="flex items-center gap-2 text-sm text-muted">
                  <span className={`size-2 rounded-full ${berth.dot}`} />
                  {berth.label}
                </dt>
                <dd className="mt-2 font-display text-3xl font-semibold">{countOf(berth.status)}</dd>
              </div>
            ))}
          </dl>

          <h3 className="mt-10 font-display text-lg font-semibold">Waiting on you</h3>

          {waitingOnYou.length === 0 ? (
            <p className="mt-2 text-muted">
              Nothing is waiting on you.{' '}
              {canEditStrategy(role) && (
                <>
                  <Link to={`/orgs/${tenantId}/strategies`} className="text-link hover:underline">
                    Start a strategy
                  </Link>{' '}
                  from a video that loses viewers.
                </>
              )}
            </p>
          ) : (
            <ul className="mt-3 divide-y rounded-xl border">
              {waitingOnYou.slice(0, 6).map((strategy) => (
                <li key={strategy.id}>
                  <Link
                    to={`/orgs/${tenantId}/strategies/${strategy.id}`}
                    className="flex items-center justify-between gap-3 px-4 py-3 hover:bg-raised"
                  >
                    <span className="min-w-0">
                      <span className="block truncate font-medium">{titleOf(strategy.videoId)}</span>
                      <span className="text-sm text-subtle">{actionFor(strategy.status)}</span>
                    </span>
                    <StatusBadge status={strategy.status} />
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </div>
  )
}
