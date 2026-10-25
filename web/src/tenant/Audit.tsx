import { useState } from 'react'
import { Link } from 'react-router-dom'
import { AUDIT_PAGE_SIZE, useAudit, type AuditEntry } from '../api/audit'
import { actorOf, describeEntry, documentIdOf } from './auditText'
import { canViewAudit } from './roles'
import { useTenant } from './useTenant'

function AuditRow({ entry, tenantId }: { entry: AuditEntry; tenantId: string }) {
  const documentId = documentIdOf(entry)
  const raw = typeof entry.data === 'string' ? entry.data : JSON.stringify(entry.data, null, 2)

  return (
    <li className="rounded border bg-white p-3">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          {/* Plain text on purpose: rejection reasons are typed by people. */}
          <p className="font-medium">{describeEntry(entry)}</p>
          <p className="text-xs text-slate-500">
            {actorOf(entry)} · {new Date(entry.recordedAtUtc).toLocaleString()}
          </p>
        </div>
        {documentId && (
          <Link
            to={`/orgs/${tenantId}/strategies/${documentId}`}
            className="text-sm text-indigo-600 hover:underline"
          >
            View strategy
          </Link>
        )}
      </div>

      <details className="mt-2">
        <summary className="cursor-pointer text-xs text-slate-500">Raw record ({entry.eventType})</summary>
        <pre className="mt-2 overflow-x-auto rounded bg-slate-50 p-2 text-xs">{raw}</pre>
      </details>
    </li>
  )
}

function AuditAdmin({ tenantId }: { tenantId: string }) {
  const [page, setPage] = useState(0)
  const [live, setLive] = useState(false)
  const audit = useAudit(tenantId, page, live)

  const entries = audit.data ?? []

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="text-lg font-semibold">Audit trail</h3>
          <p className="text-sm text-slate-600">
            Every strategy action is saved with the change itself, then copied here a few seconds later. Newest first.
          </p>
        </div>
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={live} onChange={(event) => setLive(event.target.checked)} />
          Live (every 5 seconds)
        </label>
      </div>

      {audit.isPending && <p className="mt-3 text-slate-500">Loading the audit trail…</p>}
      {audit.isError && <p className="mt-3 text-red-700">{audit.error.message}</p>}

      {audit.data && entries.length === 0 && (
        <p className="mt-3 text-slate-600">
          {page === 0
            ? 'Nothing recorded yet. Request a draft or review a strategy and an entry appears here.'
            : 'No older entries.'}
        </p>
      )}

      {entries.length > 0 && (
        <ul className="mt-3 space-y-2">
          {entries.map((entry) => (
            <AuditRow key={entry.id} entry={entry} tenantId={tenantId} />
          ))}
        </ul>
      )}

      <div className="mt-4 flex items-center gap-3 text-sm">
        <button
          className="rounded border px-3 py-1 hover:bg-slate-100 disabled:opacity-50"
          disabled={page === 0}
          onClick={() => setPage(page - 1)}
        >
          Newer
        </button>
        <span className="text-slate-500">Page {page + 1}</span>
        <button
          className="rounded border px-3 py-1 hover:bg-slate-100 disabled:opacity-50"
          disabled={entries.length < AUDIT_PAGE_SIZE}
          onClick={() => setPage(page + 1)}
        >
          Older
        </button>
        {audit.isFetching && <span className="text-slate-400">Refreshing…</span>}
      </div>
    </div>
  )
}

export function Audit() {
  const { tenantId, role } = useTenant()

  if (!canViewAudit(role)) {
    return <p className="rounded border bg-white p-4 text-slate-600">Only Admins can read the audit trail.</p>
  }

  return <AuditAdmin tenantId={tenantId} />
}