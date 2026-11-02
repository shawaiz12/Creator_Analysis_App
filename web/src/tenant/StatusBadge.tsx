import type { StrategyStatus } from '../api/strategies'

// Colour is meaning here: amber = waiting for a person, coral = sent back, green = approved.
const styles: Record<StrategyStatus, { badge: string; dot: string }> = {
  Draft: { badge: 'bg-raised text-muted', dot: 'bg-subtle' },
  PendingApproval: { badge: 'bg-pending-soft text-pending', dot: 'bg-pending' },
  NeedsRevision: { badge: 'bg-bad-soft text-bad', dot: 'bg-bad' },
  Approved: { badge: 'bg-ok-soft text-ok', dot: 'bg-ok' },
  Implemented: { badge: 'bg-link/15 text-link', dot: 'bg-link' },
}

const labels: Record<StrategyStatus, string> = {
  Draft: 'Draft',
  PendingApproval: 'Pending approval',
  NeedsRevision: 'Needs revision',
  Approved: 'Approved',
  Implemented: 'Implemented',
}

export function StatusBadge({ status }: { status: StrategyStatus }) {
  const style = styles[status]

  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium ${style.badge}`}>
      <span className={`size-1.5 rounded-full ${style.dot}`} />
      {labels[status]}
    </span>
  )
}
