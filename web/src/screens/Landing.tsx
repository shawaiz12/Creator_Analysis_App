import { DEMO_VIDEO_IDS, demoCurves, demoVideos } from '../demo/demoData'
import { clock, retentionDrops } from '../tenant/retentionDrops'

const CHART = { width: 540, height: 230, left: 40, right: 10, top: 14, bottom: 30 }

// The curve on this page is the demo's real sample data, so what a visitor sees here is what they get inside.
function HeroChart() {
  const points = demoCurves.get(DEMO_VIDEO_IDS.intros) ?? []
  const video = demoVideos.find((v) => v.id === DEMO_VIDEO_IDS.intros)
  if (points.length === 0 || !video) return null

  const { width, height, left, right, top, bottom } = CHART
  const x = (ratio: number) => left + ratio * (width - left - right)
  const y = (value: number) => top + (1 - value) * (height - top - bottom)

  const line = points
    .map((p, i) => `${i === 0 ? 'M' : 'L'}${x(p.elapsedRatio).toFixed(1)} ${y(p.audienceWatchRatio).toFixed(1)}`)
    .join(' ')
  const area = `${line} L${x(1)} ${y(0)} L${x(points[0].elapsedRatio)} ${y(0)} Z`

  const drops = retentionDrops(points, 2)
  const biggest = drops[0]
  const lampPoint = biggest ? points[Math.min(99, Math.round(biggest.endRatio * 100) - 1)] : undefined

  return (
    <svg
      viewBox={`0 0 ${width} ${height}`}
      role="img"
      aria-label={`Retention curve for the sample video "${video.title}", with the biggest drop-off marked`}
      className="w-full"
    >
      {[0, 0.5, 1].map((tick) => (
        <g key={tick}>
          <line x1={left} x2={width - right} y1={y(tick)} y2={y(tick)} className="stroke-line" strokeDasharray={tick === 0 ? undefined : '3 5'} />
          <text x={left - 8} y={y(tick) + 4} textAnchor="end" className="fill-subtle text-[11px]">
            {Math.round(tick * 100)}%
          </text>
        </g>
      ))}

      {[0, 0.5, 1].map((tick) => (
        <text key={tick} x={x(tick)} y={height - 8} textAnchor={tick === 0 ? 'start' : tick === 1 ? 'end' : 'middle'} className="fill-subtle text-[11px]">
          {clock(tick * video.durationSeconds)}
        </text>
      ))}

      {drops.map((drop) => (
        <g key={drop.startRatio}>
          <rect x={x(drop.startRatio)} y={top} width={x(drop.endRatio) - x(drop.startRatio)} height={height - top - bottom} className="fill-pending/10" />
          <text x={x(drop.startRatio) + 6} y={height - bottom - 8} className="fill-pending text-[11px] font-medium">
            {`−${Math.round(drop.pointsLost)} points`}
          </text>
        </g>
      ))}

      <path d={area} className="fill-link/10" />
      <path d={line} pathLength={1} fill="none" strokeWidth={2.5} strokeLinecap="round" strokeLinejoin="round" className="draw-curve stroke-link" />

      {lampPoint && (
        <circle cx={x(lampPoint.elapsedRatio)} cy={y(lampPoint.audienceWatchRatio)} r={5} className="lamp fill-pending stroke-surface" strokeWidth={2} />
      )}
    </svg>
  )
}

function HeroPanel() {
  const video = demoVideos.find((v) => v.id === DEMO_VIDEO_IDS.intros)

  return (
    <div className="rounded-2xl border bg-surface p-5 sm:p-6">
      <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
        <p className="font-display text-lg font-semibold">{video?.title}</p>
        <p className="text-sm text-subtle">Sample channel: Northwind Studio</p>
      </div>
      <p className="mt-1 text-sm text-muted">Share of viewers still watching, from start to end.</p>

      <div className="mt-3">
        <HeroChart />
      </div>

      <div className="mt-4 border-t pt-4">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <p className="text-sm font-medium">Draft for this video</p>
          <span className="inline-flex items-center gap-2 rounded-full bg-pending-soft px-2.5 py-1 text-xs font-medium text-pending">
            <span className="size-1.5 rounded-full bg-pending" />
            Waiting for an Admin
          </span>
        </div>
        <p className="mt-2 text-sm text-muted">
          <span className="font-medium text-ai">AI draft</span>, edited by Maya: &ldquo;Open with the result. Say the 40% figure in the
          first 5 seconds, then explain it.&rdquo;
        </p>
      </div>
    </div>
  )
}

const steps = [
  {
    role: 'Strategist asks',
    title: 'AI writes the first draft',
    text: 'It reads where viewers leave and the script, then suggests pacing fixes and thumbnail ideas.',
  },
  {
    role: 'Strategist edits',
    title: 'A person shapes it',
    text: 'Every edit is saved as a new revision. Nothing is overwritten.',
  },
  {
    role: 'Admin decides',
    title: 'Approve or send back',
    text: 'A rejection needs a reason, and the next AI draft takes that reason into account.',
  },
  {
    role: 'Editor applies',
    title: 'The team ships it',
    text: 'Editors see only approved strategies, and mark each one done when it is applied.',
  },
]

const included = [
  'A made-up channel, Northwind Studio, with six videos and their retention curves.',
  'Real AI drafts: 5 a day per visitor. Reject one with a reason and the next draft takes it into account.',
  'A role switcher, so you can see the same work as an Admin, a Strategist or an Editor.',
  'Your changes stay in this browser tab. Close it and they are gone.',
]

export function Landing({ onSignIn, onGuest }: { onSignIn: () => void; onGuest: () => void }) {
  return (
    <div>
      <section className="mx-auto grid max-w-6xl items-center gap-10 px-5 py-12 lg:grid-cols-[1fr_1.05fr] lg:gap-14 lg:py-20">
        <div>
          <h1 className="font-display text-4xl font-semibold leading-[1.05] tracking-tight sm:text-5xl lg:text-[3.4rem]">
            See where viewers leave, then fix it with a draft your team approves.
          </h1>
          <p className="mt-5 max-w-[34rem] text-lg leading-relaxed text-muted">
            Content Dock reads each video&rsquo;s retention curve, drafts script and thumbnail fixes with AI, and holds every
            suggestion until an Admin signs it off.
          </p>

          <div className="mt-8 flex flex-wrap items-center gap-3">
            <button
              type="button"
              onClick={onGuest}
              className="rounded-lg bg-primary px-5 py-3 font-medium text-on-solid hover:bg-primary-hover"
            >
              Continue as guest
            </button>
            <button
              type="button"
              onClick={onSignIn}
              className="rounded-lg border border-line-strong px-5 py-3 font-medium hover:bg-raised"
            >
              Sign in with Microsoft
            </button>
          </div>
          <p className="mt-4 max-w-[34rem] text-sm text-subtle">
            Guest mode needs no account. Sign-in is for invited team members.
          </p>
        </div>

        <HeroPanel />
      </section>

      <section className="border-y bg-rail">
        <div className="mx-auto max-w-6xl px-5 py-14">
          <h2 className="font-display text-2xl font-semibold sm:text-3xl">How a draft gets approved</h2>
          <p className="mt-2 max-w-xl text-muted">No AI suggestion is final until a person has reviewed it. Every step is recorded in an audit trail.</p>

          <ol className="mt-10 grid gap-8 sm:grid-cols-2 lg:grid-cols-4">
            {steps.map((step, index) => (
              <li key={step.title} className="relative border-t border-line-strong pt-5">
                <span className="absolute -top-px left-0 h-0.5 w-10 bg-ink" />
                <p className="font-display text-3xl font-semibold text-subtle">{index + 1}</p>
                <p className="mt-3 text-sm font-medium text-pending">{step.role}</p>
                <h3 className="mt-1 font-display text-lg font-semibold">{step.title}</h3>
                <p className="mt-2 text-sm leading-relaxed text-muted">{step.text}</p>
              </li>
            ))}
          </ol>
        </div>
      </section>

      <section className="mx-auto grid max-w-6xl gap-10 px-5 py-14 md:grid-cols-[1.4fr_1fr]">
        <div>
          <h2 className="font-display text-2xl font-semibold sm:text-3xl">What you can try as a guest</h2>
          <ul className="mt-5 space-y-3">
            {included.map((item) => (
              <li key={item} className="flex gap-3 text-muted">
                <span className="mt-2 size-1.5 shrink-0 rounded-full bg-ok" />
                {item}
              </li>
            ))}
          </ul>
        </div>

        <div className="self-start rounded-2xl border border-dashed p-5">
          <h3 className="font-display text-lg font-semibold">Not available yet</h3>
          <p className="mt-2 text-sm leading-relaxed text-muted">
            Connecting your own YouTube channel. It needs Google&rsquo;s review of the app, so the demo uses a made-up
            channel for now.
          </p>
        </div>
      </section>

      <footer className="border-t">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-5 py-6 text-sm text-subtle">
          <p>Content Dock is a portfolio project by Shawaiz.</p>
          <a
            href="https://github.com/shawaiz12/Creator_Analysis_App"
            target="_blank"
            rel="noreferrer"
            className="text-link hover:underline"
          >
            Source code on GitHub
          </a>
        </div>
      </footer>
    </div>
  )
}
