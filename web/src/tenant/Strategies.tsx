import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useVideos } from '../api/integration'
import { useCreateStrategy, useStrategies } from '../api/strategies'
import { canEditStrategy, canViewAnalytics } from './roles'
import { StatusBadge } from './StatusBadge'
import { useTenant } from './useTenant'

function NewStrategyForm() {
  const { tenantId } = useTenant()
  const videos = useVideos(tenantId)
  const create = useCreateStrategy(tenantId)
  const navigate = useNavigate()
  const [videoId, setVideoId] = useState('')

  return (
    <form
      className="mb-6 rounded border bg-white p-4"
      onSubmit={(event) => {
        event.preventDefault()
        create.mutate(videoId, {
          onSuccess: (result) => navigate(`/orgs/${tenantId}/strategies/${result.documentId}`),
        })
      }}
    >
      <label htmlFor="video" className="block text-sm font-medium">
        Start a strategy for a video
      </label>
      <div className="mt-2 flex gap-2">
        <select
          id="video"
          className="flex-1 rounded border px-3 py-2"
          value={videoId}
          onChange={(event) => setVideoId(event.target.value)}
        >
          <option value="">Choose a video…</option>
          {videos.data?.map((video) => (
            <option key={video.id} value={video.id}>
              {video.title}
            </option>
          ))}
        </select>
        <button
          type="submit"
          disabled={!videoId || create.isPending}
          className="rounded bg-indigo-600 px-4 py-2 font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
        >
          {create.isPending ? 'Creating…' : 'Create'}
        </button>
      </div>
      {videos.data?.length === 0 && (
        <p className="mt-2 text-sm text-slate-500">No videos yet. Sync a channel on the Videos tab first.</p>
      )}
      {create.isError && <p className="mt-2 text-sm text-red-700">{create.error.message}</p>}
    </form>
  )
}

export function Strategies() {
  const { tenantId, role } = useTenant()
  const strategies = useStrategies(tenantId)
  // Editors cannot read the videos list, so titles are only looked up for roles that can.
  const videos = useVideos(tenantId, canViewAnalytics(role))

  const titleOf = (videoId: string) =>
    videos.data?.find((video) => video.id.toLowerCase() === videoId.toLowerCase())?.title ??
    `Video ${videoId.slice(0, 8)}`

  return (
    <div>
      {canEditStrategy(role) && <NewStrategyForm />}

      <h3 className="text-lg font-semibold">Strategies</h3>

      {strategies.isPending && <p className="mt-2 text-slate-500">Loading strategies…</p>}
      {strategies.isError && <p className="mt-2 text-red-700">{strategies.error.message}</p>}

      {strategies.data && strategies.data.length === 0 && (
        <p className="mt-2 text-slate-600">
          {role === 'Editor' ? 'No approved strategies yet.' : 'No strategies yet. Start one above.'}
        </p>
      )}

      {strategies.data && strategies.data.length > 0 && (
        <ul className="mt-3 space-y-2">
          {strategies.data.map((strategy) => (
            <li key={strategy.id}>
              <Link
                to={`/orgs/${tenantId}/strategies/${strategy.id}`}
                className="flex items-center justify-between gap-3 rounded border bg-white p-3 hover:border-indigo-400"
              >
                <div>
                  <p className="font-medium">{titleOf(strategy.videoId)}</p>
                  <p className="text-xs text-slate-500">
                    {new Date(strategy.createdAtUtc).toLocaleDateString()} · {strategy.revisionCount}{' '}
                    {strategy.revisionCount === 1 ? 'revision' : 'revisions'}
                  </p>
                </div>
                <StatusBadge status={strategy.status} />
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}