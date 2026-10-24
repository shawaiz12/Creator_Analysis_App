import { Link, useParams } from 'react-router-dom'
import { useRetention, useVideos, type RetentionPoint } from '../api/integration'
import { useTenant } from './useTenant'

const W = 640
const H = 280
const LEFT = 48
const RIGHT = 16
const TOP = 16
const BOTTOM = 36

const percent = (value: number) => `${Math.round(value * 100)}%`

function RetentionChart({ points }: { points: RetentionPoint[] }) {
  // Watch ratio can exceed 100% when viewers re-watch, so the scale adapts.
  const maxY = Math.max(1, ...points.map((p) => p.audienceWatchRatio))
  const x = (ratio: number) => LEFT + ratio * (W - LEFT - RIGHT)
  const y = (value: number) => TOP + (1 - value / maxY) * (H - TOP - BOTTOM)

  const line = points
    .map((p, i) => `${i === 0 ? 'M' : 'L'}${x(p.elapsedRatio).toFixed(1)} ${y(p.audienceWatchRatio).toFixed(1)}`)
    .join(' ')

  const first = points[0]
  const last = points[points.length - 1]
  const area = `${line} L${x(last.elapsedRatio).toFixed(1)} ${y(0)} L${x(first.elapsedRatio).toFixed(1)} ${y(0)} Z`

  const yTicks = [0, maxY / 2, maxY]
  const xTicks = [0, 0.25, 0.5, 0.75, 1]

  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      role="img"
      aria-label="Audience retention curve: share of viewers still watching over the length of the video"
      className="w-full rounded border bg-white"
    >
      {yTicks.map((tick) => (
        <g key={tick}>
          <line x1={LEFT} x2={W - RIGHT} y1={y(tick)} y2={y(tick)} className="stroke-slate-200" />
          <text x={LEFT - 8} y={y(tick) + 4} textAnchor="end" className="fill-slate-500 text-[11px]">
            {percent(tick)}
          </text>
        </g>
      ))}

      {xTicks.map((tick) => (
        <text key={tick} x={x(tick)} y={H - 12} textAnchor="middle" className="fill-slate-500 text-[11px]">
          {percent(tick)}
        </text>
      ))}

      <path d={area} className="fill-indigo-100" />
      <path d={line} fill="none" strokeWidth={2} strokeLinejoin="round" className="stroke-indigo-600" />
    </svg>
  )
}

export function VideoRetention() {
  const { tenantId } = useTenant()
  const { videoId = '' } = useParams()
  const retention = useRetention(tenantId, videoId)
  const videos = useVideos(tenantId)

  const video = videos.data?.find((v) => v.id.toLowerCase() === videoId.toLowerCase())

  return (
    <div>
      <Link to={`/orgs/${tenantId}/videos`} className="text-sm text-indigo-600 hover:underline">
        ← Back to videos
      </Link>

      <h3 className="mt-3 text-lg font-semibold">{video?.title ?? 'Video retention'}</h3>

      {retention.isPending && <p className="mt-3 text-slate-500">Loading retention…</p>}
      {retention.isError && <p className="mt-3 text-red-700">{retention.error.message}</p>}

      {retention.isSuccess && retention.data === null && (
        <p className="mt-3 rounded border bg-white p-4 text-slate-600">
          No retention data for this video yet. Sync its channel first.
        </p>
      )}

      {retention.data && (
        <>
          <p className="mt-1 text-sm text-slate-500">
            Snapshot from {new Date(retention.data.fetchedAtUtc).toLocaleString()}. Horizontal axis: position in the
            video. Vertical axis: share of the starting audience still watching.
          </p>
          <div className="mt-3">
            <RetentionChart points={retention.data.points} />
          </div>
          <p className="mt-3 text-sm text-slate-600">
            Still watching at the end:{' '}
            <strong>{percent(retention.data.points[retention.data.points.length - 1].audienceWatchRatio)}</strong>
          </p>
        </>
      )}
    </div>
  )
}