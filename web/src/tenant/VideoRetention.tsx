import { Link, useParams } from 'react-router-dom'
import { useRetention, useVideos, type RetentionPoint } from '../api/integration'
import { clock, retentionDrops } from './retentionDrops'
import { useTenant } from './useTenant'

const W = 680
const H = 300
const LEFT = 48
const RIGHT = 16
const TOP = 16
const BOTTOM = 36

const percent = (value: number) => `${Math.round(value * 100)}%`

function RetentionChart({ points, durationSeconds }: { points: RetentionPoint[]; durationSeconds: number }) {
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
  const drops = retentionDrops(points, 2)

  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      role="img"
      aria-label="Audience retention curve: share of viewers still watching over the length of the video, with the biggest drop-offs shaded"
      className="w-full rounded-xl border bg-surface"
    >
      {yTicks.map((tick) => (
        <g key={tick}>
          <line x1={LEFT} x2={W - RIGHT} y1={y(tick)} y2={y(tick)} className="stroke-line" strokeDasharray={tick === 0 ? undefined : '3 5'} />
          <text x={LEFT - 8} y={y(tick) + 4} textAnchor="end" className="fill-subtle text-[11px]">
            {percent(tick)}
          </text>
        </g>
      ))}

      {xTicks.map((tick) => (
        <text key={tick} x={x(tick)} y={H - 12} textAnchor="middle" className="fill-subtle text-[11px]">
          {clock(tick * durationSeconds)}
        </text>
      ))}

      {drops.map((drop) => (
        <g key={drop.startRatio}>
          <rect x={x(drop.startRatio)} y={TOP} width={x(drop.endRatio) - x(drop.startRatio)} height={H - TOP - BOTTOM} className="fill-pending/10" />
          <text x={x(drop.startRatio) + 6} y={H - BOTTOM - 8} className="fill-pending text-[11px] font-medium">
            {`−${Math.round(drop.pointsLost)} points`}
          </text>
        </g>
      ))}

      <path d={area} className="fill-link/10" />
      <path d={line} fill="none" strokeWidth={2.5} strokeLinejoin="round" strokeLinecap="round" className="stroke-link" />
    </svg>
  )
}

export function VideoRetention() {
  const { tenantId } = useTenant()
  const { videoId = '' } = useParams()
  const retention = useRetention(tenantId, videoId)
  const videos = useVideos(tenantId)

  const video = videos.data?.find((v) => v.id.toLowerCase() === videoId.toLowerCase())
  const drops = retention.data && video ? retentionDrops(retention.data.points, 3) : []

  return (
    <div>
      <Link to={`/orgs/${tenantId}/videos`} className="text-sm text-link hover:underline">
        Back to videos
      </Link>

      <h3 className="mt-3 font-display text-2xl font-semibold">{video?.title ?? 'Video retention'}</h3>

      {retention.isPending && <p className="mt-3 text-subtle">Loading retention…</p>}
      {retention.isError && <p className="mt-3 text-bad">{retention.error.message}</p>}

      {retention.isSuccess && retention.data === null && (
        <p className="mt-3 rounded-xl border bg-surface p-4 text-muted">
          No retention data for this video yet. Sync its channel first.
        </p>
      )}

      {retention.data && (
        <>
          <p className="mt-1 text-sm text-subtle">
            Snapshot from {new Date(retention.data.fetchedAtUtc).toLocaleString()}. Across: position in the video. Up:
            share of the starting audience still watching.
          </p>

          <div className="mt-4">
            <RetentionChart points={retention.data.points} durationSeconds={video?.durationSeconds ?? 0} />
          </div>

          <p className="mt-4 text-muted">
            Still watching at the end:{' '}
            <strong className="text-ink">{percent(retention.data.points[retention.data.points.length - 1].audienceWatchRatio)}</strong>
          </p>

          {video && drops.length > 0 && (
            <div className="mt-6">
              <h4 className="font-display text-lg font-semibold">Where viewers leave</h4>
              <ul className="mt-2 divide-y rounded-xl border">
                {drops.map((drop) => (
                  <li key={drop.startRatio} className="flex items-center justify-between gap-3 px-4 py-3">
                    <span>
                      {clock(drop.startRatio * video.durationSeconds)} to {clock(drop.endRatio * video.durationSeconds)}
                    </span>
                    <span className="font-medium text-pending">{`−${drop.pointsLost} points`}</span>
                  </li>
                ))}
              </ul>
              <p className="mt-2 text-sm text-subtle">These are the facts the AI draft starts from.</p>
            </div>
          )}
        </>
      )}
    </div>
  )
}
