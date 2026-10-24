import type { StrategyStatus } from '../api/strategies'

const styles: Record<StrategyStatus, string> = {
  Draft: 'bg-slate-100 text-slate-700',
  PendingApproval: 'bg-amber-100 text-amber-800',
  NeedsRevision: 'bg-red-100 text-red-800',
  Approved: 'bg-green-100 text-green-800',
  Implemented: 'bg-indigo-100 text-indigo-800',
}

const labels: Record<StrategyStatus, string> = {
  Draft: 'Draft',
  PendingApproval: 'Pending approval',
  NeedsRevision: 'Needs revision',
  Approved: 'Approved',
  Implemented: 'Implemented',
}

export function StatusBadge({ status }: { status: StrategyStatus }) {
  return <span className={`rounded px-2 py-0.5 text-xs font-medium ${styles[status]}`}>{labels[status]}</span>
}