import { ApiError } from '../api/errors'
import type { Channel, Retention, RetentionPoint, SyncResult, Video } from '../api/integration'
import type { Me } from '../api/me'
import { DEMO_TENANT_ID, DEMO_USER_EMAIL, getDemoRole } from './demoMode'

const minutesAgo = (minutes: number) => new Date(Date.now() - minutes * 60_000).toISOString()
const daysAgo = (days: number) => minutesAgo(days * 24 * 60)

// ---- Sample data: a fictional channel called Northwind Studio ----

const channel: Channel = {
  id: '7f3b0000-0000-4000-8000-0000000000c1',
  youTubeChannelId: 'UC-northwind-demo',
  title: 'Northwind Studio',
  status: 'Active',
  lastSyncedAtUtc: minutesAgo(95),
}

type Drop = { at: number; size: number }

type SampleVideo = Omit<Video, 'publishedAtUtc'> & { daysAgo: number; floor: number; drops: Drop[] }

const sampleVideos: SampleVideo[] = [
  { id: '7f3b0001-0000-4000-8000-000000000001', youTubeVideoId: 'nw-demo-001', title: 'How I Edit a 10-Minute Video in 90 Minutes', daysAgo: 4, durationSeconds: 742, hasTranscript: true, floor: 0.3, drops: [{ at: 0.06, size: 0.09 }, { at: 0.47, size: 0.07 }] },
  { id: '7f3b0002-0000-4000-8000-000000000002', youTubeVideoId: 'nw-demo-002', title: 'The $300 Studio Setup (Everything I Use)', daysAgo: 11, durationSeconds: 596, hasTranscript: true, floor: 0.34, drops: [{ at: 0.12, size: 0.08 }, { at: 0.71, size: 0.06 }] },
  { id: '7f3b0003-0000-4000-8000-000000000003', youTubeVideoId: 'nw-demo-003', title: 'Why Your Intros Lose 40% of Viewers', daysAgo: 19, durationSeconds: 488, hasTranscript: true, floor: 0.36, drops: [{ at: 0.04, size: 0.1 }, { at: 0.33, size: 0.05 }] },
  { id: '7f3b0004-0000-4000-8000-000000000004', youTubeVideoId: 'nw-demo-004', title: 'Thumbnail Mistakes I Made for 3 Years', daysAgo: 33, durationSeconds: 531, hasTranscript: false, floor: 0.28, drops: [{ at: 0.18, size: 0.07 }] },
  { id: '7f3b0005-0000-4000-8000-000000000005', youTubeVideoId: 'nw-demo-005', title: 'Cheap Mics vs Expensive Mics: Blind Test', daysAgo: 47, durationSeconds: 874, hasTranscript: true, floor: 0.32, drops: [{ at: 0.09, size: 0.08 }, { at: 0.55, size: 0.09 }] },
  { id: '7f3b0006-0000-4000-8000-000000000006', youTubeVideoId: 'nw-demo-006', title: 'Monthly Channel Report: What Actually Grew', daysAgo: 62, durationSeconds: 948, hasTranscript: true, floor: 0.26, drops: [{ at: 0.14, size: 0.09 }, { at: 0.62, size: 0.07 }] },
]

function buildCurve(drops: Drop[], floor: number): RetentionPoint[] {
  const points: RetentionPoint[] = []

  for (let i = 0; i < 100; i++) {
    const elapsedRatio = (i + 1) / 100
    let watch = floor + (1 - floor) * Math.exp(-2.4 * elapsedRatio)

    for (const drop of drops) {
      // Each drop is a smooth step over about 3% of the video.
      const progress = Math.min(1, Math.max(0, (elapsedRatio - drop.at) / 0.03))
      watch -= drop.size * progress * progress * (3 - 2 * progress)
    }

    // A little wobble so the curve does not look machine-made.
    watch += Math.sin(i * 1.7) * 0.004

    points.push({ elapsedRatio, audienceWatchRatio: Math.min(1, Math.max(0.02, watch)) })
  }

  return points
}

const videos: Video[] = sampleVideos.map((v) => ({
  id: v.id,
  youTubeVideoId: v.youTubeVideoId,
  title: v.title,
  publishedAtUtc: daysAgo(v.daysAgo),
  durationSeconds: v.durationSeconds,
  hasTranscript: v.hasTranscript,
}))

const curves = new Map<string, RetentionPoint[]>(sampleVideos.map((v) => [v.id, buildCurve(v.drops, v.floor)]))

// ---- The fake server ----

const PAUSE_MS = 220
const pause = () => new Promise((resolve) => setTimeout(resolve, PAUSE_MS))

export async function demoFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  await pause()
  const method = (init.method ?? 'GET').toUpperCase()
  const url = path.split('?')[0]

  return structuredClone(route(method, url)) as T
}

function me(): Me {
  return {
    email: DEMO_USER_EMAIL,
    organizations: [{ organizationId: DEMO_TENANT_ID, organizationName: 'Northwind Studio', role: getDemoRole() }],
  }
}

function route(method: string, url: string): unknown {
  if (method === 'GET' && url === '/api/me') return me()

  if (method === 'POST' && url === '/api/onboarding/organizations') {
    throw new ApiError(403, 'Creating organizations is turned off in the demo.')
  }

  const prefix = `/api/${DEMO_TENANT_ID}`
  // Like the real API: anything outside the one demo organization looks like it does not exist.
  if (!url.toLowerCase().startsWith(`${prefix}/`)) throw new ApiError(404, 'Not found.')

  const rest = url.slice(prefix.length)
  const role = getDemoRole()
  const canSeeAnalytics = role === 'Admin' || role === 'Strategist'

  if (method === 'GET' && rest === '/channels') {
    if (!canSeeAnalytics) throw new ApiError(403, 'Your role cannot view channels.')
    return [channel]
  }

  if (method === 'POST' && rest === '/channels') {
    throw new ApiError(403, 'Registering a channel is turned off in the demo.')
  }

  if (method === 'POST' && /^\/channels\/[^/]+\/sync$/.test(rest)) {
    if (!canSeeAnalytics) throw new ApiError(403, 'Your role cannot sync channels.')
    channel.lastSyncedAtUtc = new Date().toISOString()
    const result: SyncResult = { videosAdded: 0, videosUpdated: videos.length, snapshotsStored: videos.length }
    return result
  }

  if (method === 'GET' && rest === '/videos') {
    if (!canSeeAnalytics) throw new ApiError(403, 'Your role cannot view videos.')
    return [...videos].sort((a, b) => b.publishedAtUtc.localeCompare(a.publishedAtUtc))
  }

  const retention = /^\/videos\/([^/]+)\/retention$/.exec(rest)
  if (method === 'GET' && retention) {
    if (!canSeeAnalytics) throw new ApiError(403, 'Your role cannot view retention.')
    const points = curves.get(retention[1])
    if (!points) throw new ApiError(404, 'Not found.')
    const result: Retention = { videoId: retention[1], fetchedAtUtc: new Date().toISOString(), points }
    return result
  }

  // Strategies, team and audit arrive in the next slice.
  throw new ApiError(404, 'This part of the demo is not built yet.')
}