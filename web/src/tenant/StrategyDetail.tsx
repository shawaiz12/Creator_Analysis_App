import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ApiError } from '../api/apiFetch'
import { useVideos } from '../api/integration'
import {
  useAddRevision,
  useApprove,
  useGenerate,
  useGeneration,
  useMarkImplemented,
  useRefreshWhenGenerationEnds,
  useReject,
  useStrategy,
  useSubmit,
  type GenerationRequest,
  type Strategy,
} from '../api/strategies'
import { canEditStrategy, canMarkImplemented, canReview, canViewAnalytics } from './roles'
import { StatusBadge } from './StatusBadge'
import { useTenant } from './useTenant'

const formatDate = (value: string) => new Date(value).toLocaleString()

function ErrorLine({ error }: { error: Error | null }) {
  if (!error) return null
  return <p className="mt-2 text-sm text-red-700">{error.message}</p>
}

function GenerationPanel({
  tenantId,
  strategyId,
  requests,
  generating,
  editable,
}: {
  tenantId: string
  strategyId: string
  requests: GenerationRequest[]
  generating: boolean
  editable: boolean
}) {
  const generate = useGenerate(tenantId, strategyId)

  if (!editable && requests.length === 0) return null

  return (
    <section className="mt-6 rounded border bg-white p-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h4 className="font-semibold">AI drafts</h4>
          <p className="text-sm text-slate-600">
            The draft is written in the background and appears here. It still needs your edits and an Admin's
            approval.
          </p>
        </div>
        {editable && (
          <button
            className="shrink-0 rounded bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
            disabled={generate.isPending || generating}
            onClick={() => generate.mutate()}
          >
            {generating ? 'Generating…' : 'Generate AI draft'}
          </button>
        )}
      </div>

      <ErrorLine error={generate.error} />

      {requests.length > 0 && (
        <ul className="mt-3 space-y-1 text-sm">
          {requests.map((request) => (
            <li key={request.id} className="flex flex-wrap gap-x-2 text-slate-600">
              <span className="text-slate-500">{formatDate(request.requestedAtUtc)}</span>
              <span>
                {request.status === 'Pending' && 'Generating…'}
                {request.status === 'Succeeded' && 'Draft added'}
                {request.status === 'Failed' && `Failed${request.error ? `: ${request.error}` : ''}`}
              </span>
              {request.model && <span className="text-slate-400">({request.model})</span>}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

function RevisionEditor({ strategy, tenantId }: { strategy: Strategy; tenantId: string }) {
  const add = useAddRevision(tenantId, strategy.id)
  const current = strategy.currentRevision?.content ?? ''
  const [content, setContent] = useState(current)
  const unchanged = content.trim() === current.trim()

  return (
    <section className="mt-6 rounded border bg-white p-4">
      <h4 className="font-semibold">Edit</h4>
      <p className="text-sm text-slate-600">Saving creates a new revision. Earlier revisions are never overwritten.</p>
      <textarea
        className="mt-3 w-full rounded border px-3 py-2 font-sans text-sm"
        rows={12}
        maxLength={50000}
        value={content}
        onChange={(event) => setContent(event.target.value)}
      />
      <button
        className="mt-2 rounded border px-4 py-2 text-sm font-medium hover:bg-slate-100 disabled:opacity-50"
        disabled={add.isPending || !content.trim() || unchanged}
        onClick={() => add.mutate(content)}
      >
        {add.isPending ? 'Saving…' : 'Save as new revision'}
      </button>
      <ErrorLine error={add.error} />
    </section>
  )
}

function SubmitBar({ strategy, tenantId }: { strategy: Strategy; tenantId: string }) {
  const submit = useSubmit(tenantId, strategy.id)

  return (
    <section className="mt-6 rounded border bg-white p-4">
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm text-slate-600">
          When the content is ready, send it to an Admin for approval. It is locked while under review. Unsaved edits
          are not included, so save a revision first.
        </p>
        <button
          className="shrink-0 rounded bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
          disabled={!strategy.currentRevision || submit.isPending}
          onClick={() => submit.mutate()}
        >
          {submit.isPending ? 'Submitting…' : 'Submit for approval'}
        </button>
      </div>
      <ErrorLine error={submit.error} />
    </section>
  )
}

function ReviewPanel({ strategy, tenantId }: { strategy: Strategy; tenantId: string }) {
  const approve = useApprove(tenantId, strategy.id)
  const reject = useReject(tenantId, strategy.id)
  const [reason, setReason] = useState('')
  const revision = strategy.currentRevision

  if (!revision) return null

  const busy = approve.isPending || reject.isPending

  return (
    <section className="mt-6 rounded border-2 border-amber-300 bg-amber-50 p-4">
      <h4 className="font-semibold">Your review</h4>
      <p className="mt-1 text-sm text-slate-700">
        You are reviewing <strong>revision {revision.versionNumber}</strong>, shown above. Approval applies to exactly
        this revision. If the strategy changes before you decide, the approval is refused.
      </p>

      <button
        className="mt-3 rounded bg-green-700 px-4 py-2 text-sm font-medium text-white hover:bg-green-800 disabled:opacity-50"
        disabled={busy}
        onClick={() => approve.mutate(revision.id)}
      >
        {approve.isPending ? 'Approving…' : `Approve revision ${revision.versionNumber}`}
      </button>
      <ErrorLine error={approve.error} />

      <div className="mt-5 border-t border-amber-200 pt-4">
        <label htmlFor="reason" className="block text-sm font-medium">
          Or send it back with a reason (required)
        </label>
        <p className="text-xs text-slate-600">The reason is kept in the history and given to the next AI draft as context.</p>
        <textarea
          id="reason"
          className="mt-2 w-full rounded border bg-white px-3 py-2 text-sm"
          rows={3}
          value={reason}
          onChange={(event) => setReason(event.target.value)}
        />
        <button
          className="mt-2 rounded bg-red-700 px-4 py-2 text-sm font-medium text-white hover:bg-red-800 disabled:opacity-50"
          disabled={busy || !reason.trim()}
          onClick={() =>
            reject.mutate({ revisionId: revision.id, reason: reason.trim() }, { onSuccess: () => setReason('') })
          }
        >
          {reject.isPending ? 'Sending back…' : 'Reject and request changes'}
        </button>
        <ErrorLine error={reject.error} />
      </div>
    </section>
  )
}

function ImplementPanel({ strategy, tenantId }: { strategy: Strategy; tenantId: string }) {
  const implemented = useMarkImplemented(tenantId, strategy.id)

  return (
    <section className="mt-6 rounded border bg-white p-4">
      <p className="text-sm text-slate-600">This strategy is approved. Once the team has applied it, mark it as done.</p>
      <button
        className="mt-3 rounded bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
        disabled={implemented.isPending}
        onClick={() => implemented.mutate()}
      >
        {implemented.isPending ? 'Saving…' : 'Mark as implemented'}
      </button>
      <ErrorLine error={implemented.error} />
    </section>
  )
}

export function StrategyDetail() {
  const { tenantId, role } = useTenant()
  const { strategyId = '' } = useParams()

  const strategy = useStrategy(tenantId, strategyId)
  const videos = useVideos(tenantId, canViewAnalytics(role))
  const generation = useGeneration(tenantId, strategyId, canEditStrategy(role))

  const requests = generation.data ?? []
  const generating = requests.some((request) => request.status === 'Pending')
  useRefreshWhenGenerationEnds(tenantId, strategyId, generating)

  const back = (
    <Link to={`/orgs/${tenantId}/strategies`} className="text-sm text-indigo-600 hover:underline">
      ← Back to strategies
    </Link>
  )

  if (strategy.isPending) return <p className="text-slate-500">Loading strategy…</p>

  if (strategy.isError) {
    const missing = strategy.error instanceof ApiError && strategy.error.status === 404
    return (
      <div>
        {back}
        <p className="mt-3 rounded border bg-white p-4 text-slate-700">
          {missing ? 'This strategy does not exist, or you cannot see it.' : strategy.error.message}
        </p>
      </div>
    )
  }

  const data = strategy.data
  const revision = data.currentRevision
  const editable = data.status === 'Draft' || data.status === 'NeedsRevision'
  const title =
    videos.data?.find((video) => video.id.toLowerCase() === data.videoId.toLowerCase())?.title ??
    `Video ${data.videoId.slice(0, 8)}`
  const lastRejection = data.reviews.filter((review) => review.decision === 'Rejected').slice(-1)[0]

  return (
    <div>
      {back}

      <div className="mt-3 flex items-center justify-between gap-3">
        <h3 className="text-lg font-semibold">{title}</h3>
        <StatusBadge status={data.status} />
      </div>

      {data.status === 'NeedsRevision' && lastRejection && (
        <p className="mt-3 rounded border border-red-300 bg-red-50 p-3 text-sm text-red-900">
          Sent back by an Admin: {lastRejection.reason}
        </p>
      )}

      <section className="mt-6 rounded border bg-white p-4">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h4 className="font-semibold">Current revision{revision ? ` (v${revision.versionNumber})` : ''}</h4>
          {revision && (
            <span className="text-xs text-slate-500">
              {revision.origin === 'Ai' ? 'AI draft' : 'Human edit'} · {formatDate(revision.createdAtUtc)}
            </span>
          )}
        </div>
        {revision ? (
          // Plain text on purpose: AI output is untrusted, so it is never rendered as HTML.
          <pre className="mt-3 whitespace-pre-wrap break-words font-sans text-sm">{revision.content}</pre>
        ) : (
          <p className="mt-3 text-sm text-slate-600">No content yet. Generate an AI draft or write one yourself.</p>
        )}
      </section>

      {data.reviews.length > 0 && (
        <section className="mt-6">
          <h4 className="font-semibold">Review history</h4>
          <ul className="mt-2 space-y-2">
            {data.reviews.map((review) => (
              <li key={review.id} className="rounded border bg-white p-3 text-sm">
                <span className={`font-medium ${review.decision === 'Approved' ? 'text-green-800' : 'text-red-800'}`}>
                  {review.decision}
                </span>
                <span className="text-slate-500"> · {formatDate(review.reviewedAtUtc)}</span>
                {review.reason && <p className="mt-1 text-slate-700">Reason: {review.reason}</p>}
              </li>
            ))}
          </ul>
        </section>
      )}

      {canEditStrategy(role) && (
        <GenerationPanel
          tenantId={tenantId}
          strategyId={data.id}
          requests={requests}
          generating={generating}
          editable={editable}
        />
      )}

      {canEditStrategy(role) && editable && (
        <>
          {/* The key resets the editor whenever a new revision (for example an AI draft) arrives. */}
          <RevisionEditor key={revision?.id ?? 'none'} strategy={data} tenantId={tenantId} />
          <SubmitBar strategy={data} tenantId={tenantId} />
        </>
      )}

      {canReview(role) && data.status === 'PendingApproval' && <ReviewPanel strategy={data} tenantId={tenantId} />}

      {canMarkImplemented(role) && data.status === 'Approved' && <ImplementPanel strategy={data} tenantId={tenantId} />}
    </div>
  )
}