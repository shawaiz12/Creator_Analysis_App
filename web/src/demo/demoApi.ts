import { useSyncExternalStore } from 'react'
import type { AuditEntry } from '../api/audit'
import { ApiError } from '../api/errors'
import type { Retention, SyncResult } from '../api/integration'
import type { Me } from '../api/me'
import type {
  GenerationRequest,
  Revision,
  Review,
  Strategy,
  StrategyStatus,
  StrategySummary,
} from '../api/strategies'
import type { Invitation, Member } from '../api/team'
import { DEMO_VIDEO_IDS, demoChannel, demoCurves, demoTranscripts, demoVideos } from './demoData'
import { DEMO_TENANT_ID, DEMO_USER_EMAIL, DEMO_USER_ID, getDemoRole, type DemoRole } from './demoMode'

// Shown to the visitor only. The server's own limit is what is actually enforced.
export const GUEST_DAILY_AI_LIMIT = 5

const STATE_KEY = 'contentdock.demo.state'
const PAUSE_MS = 220

const AVA_ID = '0d3a1f6e-8c44-4b6a-9a51-3f7c2b9d1e21'
const MAYA_ID = '0d3a1f6e-8c44-4b6a-9a51-3f7c2b9d1e22'
const LEO_ID = '0d3a1f6e-8c44-4b6a-9a51-3f7c2b9d1e23'

const TEAMMATES: Member[] = [
  { userId: AVA_ID, email: 'ava@northwind.example', role: 'Admin' },
  { userId: MAYA_ID, email: 'maya@northwind.example', role: 'Strategist' },
  { userId: LEO_ID, email: 'leo@northwind.example', role: 'Editor' },
]

// Pre-written examples that make the demo look lived in. Drafts you generate yourself are real AI output.
const SAMPLE_INTROS_DRAFT = `What the data shows
Viewers leave fastest in the first 20 seconds (about 10 points lost around 0:20) and again near 2:40 (about 5 points). The intro spends its first 18 seconds on channel history before the first useful sentence.

Script pacing fixes
1. Open with the result: say the 40% figure in the first 5 seconds, then explain it.
2. Move the "who I am" line to after the first example.
3. Cut the history of YouTube intros to one sentence, or drop it.

Thumbnail concepts
1. A flat line chart with a sharp drop and the text "-40% in 10 seconds".
2. Split screen: a long intro versus a one-line intro, with a red cross over the long one.`

const SAMPLE_SETUP_DRAFT = `What the data shows
About 8 points of viewers are lost around 1:10, right after the personal backstory, and 6 more near 7:00, where the affiliate link list starts.

Script pacing fixes
1. Replace the 2016 backstory with a single line and show the finished studio first.
2. Put the full gear list in the description and show only the three key items on screen.

Thumbnail concepts
1. The finished desk with a big "$300" price tag.
2. Before and after: a messy corner versus the finished setup.`

// ---- The fake server's data, kept in this browser tab only ----

type StrategyRecord = {
  id: string
  videoId: string
  status: StrategyStatus
  createdAtUtc: string
  revisions: Revision[]
  reviews: Review[]
}

type State = {
  strategies: StrategyRecord[]
  generations: Record<string, GenerationRequest[]>
  audit: AuditEntry[]
  invitations: Invitation[]
  aiUsed: number
}

const uid = (): string =>
  globalThis.crypto?.randomUUID?.() ?? `${Date.now().toString(16)}-${Math.random().toString(16).slice(2)}`

const stamp = (daysBack: number, plusMinutes = 0): string =>
  new Date(Date.now() - daysBack * 86_400_000 + plusMinutes * 60_000).toISOString()

function pushAudit(target: State, eventType: string, data: Record<string, unknown>, at = new Date().toISOString()) {
  target.audit.push({ id: uid(), eventType, recordedAtUtc: at, data })
}

function seed(): State {
  const s: State = { strategies: [], generations: {}, audit: [], invitations: [], aiUsed: 0 }

  // An approved strategy: AI draft, a human edit, then an Admin's approval.
  const approvedId = uid()
  const approvedRev1: Revision = {
    id: uid(),
    versionNumber: 1,
    content: SAMPLE_INTROS_DRAFT,
    origin: 'Ai',
    authorUserId: null,
    createdAtUtc: stamp(6, 2),
  }
  const approvedRev2: Revision = {
    id: uid(),
    versionNumber: 2,
    content: `${SAMPLE_INTROS_DRAFT}\n\nEditor note: keep the new intro under 10 seconds.`,
    origin: 'Human',
    authorUserId: MAYA_ID,
    createdAtUtc: stamp(5),
  }
  s.strategies.push({
    id: approvedId,
    videoId: DEMO_VIDEO_IDS.intros,
    status: 'Approved',
    createdAtUtc: stamp(6),
    revisions: [approvedRev1, approvedRev2],
    reviews: [
      {
        id: uid(),
        revisionId: approvedRev2.id,
        reviewerUserId: AVA_ID,
        decision: 'Approved',
        reason: null,
        reviewedAtUtc: stamp(4),
      },
    ],
  })
  s.generations[approvedId] = [
    {
      id: uid(),
      status: 'Succeeded',
      attempts: 1,
      requestedAtUtc: stamp(6),
      completedAtUtc: stamp(6, 2),
      error: null,
      model: 'sample draft',
      promptVersion: null,
    },
  ]
  pushAudit(s, 'StrategyGenerationRequestedEvent', { documentId: approvedId, requestedByUserId: MAYA_ID }, stamp(6))
  pushAudit(s, 'StrategyRevisionAddedEvent', { documentId: approvedId, versionNumber: 1, origin: 'Ai' }, stamp(6, 2))
  pushAudit(
    s,
    'StrategyRevisionAddedEvent',
    { documentId: approvedId, versionNumber: 2, origin: 'Human', authorUserId: MAYA_ID },
    stamp(5),
  )
  pushAudit(s, 'StrategySubmittedEvent', { documentId: approvedId, submittedByUserId: MAYA_ID }, stamp(5, 30))
  pushAudit(s, 'StrategyApprovedEvent', { documentId: approvedId, reviewerId: AVA_ID }, stamp(4))

  // One waiting for approval, so an Admin has something to review straight away.
  const pendingId = uid()
  s.strategies.push({
    id: pendingId,
    videoId: DEMO_VIDEO_IDS.setup,
    status: 'PendingApproval',
    createdAtUtc: stamp(2),
    revisions: [
      {
        id: uid(),
        versionNumber: 1,
        content: SAMPLE_SETUP_DRAFT,
        origin: 'Ai',
        authorUserId: null,
        createdAtUtc: stamp(2, 2),
      },
    ],
    reviews: [],
  })
  s.generations[pendingId] = [
    {
      id: uid(),
      status: 'Succeeded',
      attempts: 1,
      requestedAtUtc: stamp(2),
      completedAtUtc: stamp(2, 2),
      error: null,
      model: 'sample draft',
      promptVersion: null,
    },
  ]
  pushAudit(s, 'StrategyGenerationRequestedEvent', { documentId: pendingId, requestedByUserId: MAYA_ID }, stamp(2))
  pushAudit(s, 'StrategyRevisionAddedEvent', { documentId: pendingId, versionNumber: 1, origin: 'Ai' }, stamp(2, 2))
  pushAudit(s, 'StrategySubmittedEvent', { documentId: pendingId, submittedByUserId: MAYA_ID }, stamp(1, 600))

  return s
}

function load(): State {
  try {
    const raw = sessionStorage.getItem(STATE_KEY)
    if (raw) {
      const parsed = JSON.parse(raw) as State
      // A page reload loses any request that was still running, so do not leave it spinning forever.
      for (const requests of Object.values(parsed.generations)) {
        for (const request of requests) {
          if (request.status === 'Pending') {
            request.status = 'Failed'
            request.error = 'Interrupted by a page reload.'
            request.completedAtUtc = new Date().toISOString()
          }
        }
      }
      return parsed
    }
  } catch {
    // Unreadable or blocked storage: start from a fresh copy of the sample data.
  }
  return seed()
}

let state: State = load()

const listeners = new Set<() => void>()

function commit() {
  try {
    sessionStorage.setItem(STATE_KEY, JSON.stringify(state))
  } catch {
    // Storage blocked: the demo keeps working in memory for this visit.
  }
  listeners.forEach((listener) => listener())
}

export function resetDemoState() {
  state = seed()
  commit()
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

export const useDemoAiLeft = () =>
  useSyncExternalStore(subscribe, () => Math.max(0, GUEST_DAILY_AI_LIMIT - state.aiUsed))

// ---- The one real network call: a draft from the rate-limited guest endpoint ----

type GuestDraft = { content: string; model: string; promptVersion: string }

async function requestGuestDraft(body: unknown): Promise<GuestDraft> {
  const controller = new AbortController()
  const timer = setTimeout(() => controller.abort(), 100_000)

  try {
    const response = await fetch(`${import.meta.env.VITE_API_BASE_URL}/api/guest/insights`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
      signal: controller.signal,
    })

    if (!response.ok) {
      let message = `The AI request failed (${response.status}).`
      try {
        const data = await response.json()
        if (typeof data?.error === 'string') message = data.error
      } catch {
        // No JSON body; keep the generic message.
      }
      throw new Error(message)
    }

    return (await response.json()) as GuestDraft
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw new Error('The AI took too long to answer. Please try again.', { cause: error })
    }
    if (error instanceof TypeError) {
      throw new Error('Could not reach the server. It may be waking up, so please try again in a minute.', {
        cause: error,
      })
    }
    throw error
  } finally {
    clearTimeout(timer)
  }
}

const isEditable = (status: StrategyStatus) => status === 'Draft' || status === 'NeedsRevision'

function addRevision(record: StrategyRecord, content: string, origin: 'Ai' | 'Human', authorUserId: string | null) {
  const revision: Revision = {
    id: uid(),
    versionNumber: record.revisions.length + 1,
    content,
    origin,
    authorUserId,
    createdAtUtc: new Date().toISOString(),
  }
  record.revisions.push(revision)

  pushAudit(state, 'StrategyRevisionAddedEvent', {
    documentId: record.id,
    versionNumber: revision.versionNumber,
    origin,
    authorUserId,
  })

  return revision
}

// Runs in the background after "Generate" returns, like the real API's worker.
async function runGeneration(strategyId: string, requestId: string) {
  const record = state.strategies.find((s) => s.id === strategyId)
  const video = demoVideos.find((v) => v.id === record?.videoId)
  const curve = demoCurves.get(record?.videoId ?? '')
  if (!record || !video || !curve) return

  // Earlier rejection reasons go to the next draft as context, newest first.
  const reasons = record.reviews
    .filter((review) => review.decision === 'Rejected' && review.reason)
    .map((review) => (review.reason as string).slice(0, 500))
    .reverse()
    .slice(0, 5)

  const body = {
    videoTitle: video.title,
    durationSeconds: video.durationSeconds,
    transcript: demoTranscripts.get(video.id) ?? null,
    retention: curve.map((point) => Math.round(point.audienceWatchRatio * 10_000) / 10_000),
    rejectionReasons: reasons,
  }

  let draft: GuestDraft | undefined
  let failure = 'The draft could not be generated.'

  try {
    draft = await requestGuestDraft(body)
  } catch (error) {
    if (error instanceof Error) failure = error.message
  }

  // The visitor may have reset the demo while we waited.
  const request = state.generations[strategyId]?.find((r) => r.id === requestId)
  const current = state.strategies.find((s) => s.id === strategyId)
  if (!request || !current) return

  request.completedAtUtc = new Date().toISOString()

  if (draft && !isEditable(current.status)) {
    request.status = 'Failed'
    request.error = 'The strategy was submitted while the draft was being written.'
  } else if (draft) {
    request.status = 'Succeeded'
    request.model = draft.model
    request.promptVersion = draft.promptVersion
    addRevision(current, draft.content, 'Ai', null)
  } else {
    request.status = 'Failed'
    request.error = failure
  }

  commit()
}

// ---- The fake server's routes ----

const pause = () => new Promise((resolve) => setTimeout(resolve, PAUSE_MS))

function parseBody(body: BodyInit | null | undefined): Record<string, unknown> {
  if (typeof body !== 'string') return {}
  try {
    const parsed: unknown = JSON.parse(body)
    return parsed !== null && typeof parsed === 'object' ? (parsed as Record<string, unknown>) : {}
  } catch {
    return {}
  }
}

const text = (value: unknown) => (typeof value === 'string' ? value.trim() : '')

function must(allowed: boolean, message: string) {
  if (!allowed) throw new ApiError(403, message)
}

const canEdit = (role: DemoRole) => role === 'Admin' || role === 'Strategist'
const canViewAnalytics = canEdit

export async function demoFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  await pause()
  const method = (init.method ?? 'GET').toUpperCase()
  const [url = '', queryText = ''] = path.split('?')
  const body = parseBody(init.body)

  return structuredClone(route(method, url, new URLSearchParams(queryText), body)) as T
}

function me(): Me {
  return {
    email: DEMO_USER_EMAIL,
    organizations: [{ organizationId: DEMO_TENANT_ID, organizationName: 'Northwind Studio', role: getDemoRole() }],
  }
}

function route(method: string, url: string, query: URLSearchParams, body: Record<string, unknown>): unknown {
  if (method === 'GET' && url === '/api/me') return me()

  if (method === 'POST' && url === '/api/onboarding/organizations') {
    throw new ApiError(403, 'Creating organizations is turned off in the demo.')
  }

  if (method === 'POST' && /^\/api\/invitations\/[^/]+\/accept$/.test(url)) {
    throw new ApiError(403, 'Accepting invitations is turned off in the demo.')
  }

  const prefix = `/api/${DEMO_TENANT_ID}`
  // Like the real API: anything outside the one demo organization looks like it does not exist.
  if (!url.toLowerCase().startsWith(`${prefix}/`)) throw new ApiError(404, 'Not found.')

  const rest = url.slice(prefix.length)
  const role = getDemoRole()

  // Channels, videos, retention
  if (method === 'GET' && rest === '/channels') {
    must(canViewAnalytics(role), 'Your role cannot view channels.')
    return [demoChannel]
  }

  if (method === 'POST' && rest === '/channels') {
    throw new ApiError(403, 'Registering a channel is turned off in the demo.')
  }

  if (method === 'POST' && /^\/channels\/[^/]+\/sync$/.test(rest)) {
    must(canViewAnalytics(role), 'Your role cannot sync channels.')
    demoChannel.lastSyncedAtUtc = new Date().toISOString()
    const result: SyncResult = { videosAdded: 0, videosUpdated: demoVideos.length, snapshotsStored: demoVideos.length }
    return result
  }

  if (method === 'GET' && rest === '/videos') {
    must(canViewAnalytics(role), 'Your role cannot view videos.')
    return [...demoVideos].sort((a, b) => b.publishedAtUtc.localeCompare(a.publishedAtUtc))
  }

  const retention = /^\/videos\/([^/]+)\/retention$/.exec(rest)
  if (method === 'GET' && retention) {
    must(canViewAnalytics(role), 'Your role cannot view retention.')
    const videoId = retention[1] ?? ''
    const points = demoCurves.get(videoId)
    if (!points) throw new ApiError(404, 'Not found.')
    const result: Retention = { videoId, fetchedAtUtc: new Date().toISOString(), points }
    return result
  }

  // Strategies
  const strategyRoute =
    /^\/strategies(?:\/([^/]+)(?:\/(generation|revisions|submit|generate|approve|reject|implemented))?)?$/.exec(rest)
  if (strategyRoute) return strategies(method, strategyRoute[1], strategyRoute[2], body, role)

  // Team
  if (method === 'GET' && rest === '/members') {
    return [{ userId: DEMO_USER_ID, email: DEMO_USER_EMAIL, role }, ...TEAMMATES]
  }

  if (rest === '/invitations' || /^\/invitations\/[^/]+\/revoke$/.test(rest)) {
    must(role === 'Admin', 'Only Admins can manage the team.')
    return invitations(method, rest, body)
  }

  // Audit trail
  if (method === 'GET' && rest === '/audit') {
    must(role === 'Admin', 'Only Admins can read the audit trail.')
    const take = clampInt(query.get('take'), 20, 1, 100)
    const skip = clampInt(query.get('skip'), 0, 0, 10_000)
    return [...state.audit].sort((a, b) => b.recordedAtUtc.localeCompare(a.recordedAtUtc)).slice(skip, skip + take)
  }

  throw new ApiError(404, 'Not found.')
}

function clampInt(value: string | null, fallback: number, min: number, max: number) {
  const parsed = Number.parseInt(value ?? '', 10)
  return Number.isNaN(parsed) ? fallback : Math.min(max, Math.max(min, parsed))
}

function toStrategy(record: StrategyRecord): Strategy {
  return {
    id: record.id,
    videoId: record.videoId,
    status: record.status,
    createdAtUtc: record.createdAtUtc,
    currentRevision: record.revisions.at(-1) ?? null,
    reviews: record.reviews,
  }
}

function toSummary(record: StrategyRecord): StrategySummary {
  return {
    id: record.id,
    videoId: record.videoId,
    status: record.status,
    createdAtUtc: record.createdAtUtc,
    revisionCount: record.revisions.length,
  }
}

// Editors only ever see strategies that have been approved.
const visibleToEditor = (status: StrategyStatus) => status === 'Approved' || status === 'Implemented'

function strategies(
  method: string,
  id: string | undefined,
  action: string | undefined,
  body: Record<string, unknown>,
  role: DemoRole,
): unknown {
  if (!id) {
    if (method === 'GET') {
      return state.strategies
        .filter((record) => role !== 'Editor' || visibleToEditor(record.status))
        .sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc))
        .map(toSummary)
    }

    if (method === 'POST') {
      must(canEdit(role), 'Your role cannot start strategies.')
      const videoId = text(body.videoId)
      if (!demoVideos.some((video) => video.id === videoId)) throw new ApiError(400, 'That video does not exist.')

      const record: StrategyRecord = {
        id: uid(),
        videoId,
        status: 'Draft',
        createdAtUtc: new Date().toISOString(),
        revisions: [],
        reviews: [],
      }
      state.strategies.push(record)
      commit()
      return { documentId: record.id }
    }

    throw new ApiError(404, 'Not found.')
  }

  const record = state.strategies.find((s) => s.id.toLowerCase() === id.toLowerCase())
  if (!record || (role === 'Editor' && !visibleToEditor(record.status))) throw new ApiError(404, 'Not found.')

  if (method === 'GET' && !action) return toStrategy(record)

  if (method === 'GET' && action === 'generation') {
    must(canEdit(role), 'Your role cannot see draft requests.')
    return state.generations[record.id] ?? []
  }

  if (method !== 'POST') throw new ApiError(404, 'Not found.')

  const current = record.revisions.at(-1)

  switch (action) {
    case 'revisions': {
      must(canEdit(role), 'Your role cannot edit strategies.')
      if (!isEditable(record.status)) throw new ApiError(409, 'This strategy can no longer be edited.')
      const content = text(body.content)
      if (!content || content.length > 50_000) throw new ApiError(400, 'The content is required and limited to 50,000 characters.')
      const revision = addRevision(record, content, 'Human', DEMO_USER_ID)
      commit()
      return revision
    }

    case 'submit': {
      must(canEdit(role), 'Your role cannot submit strategies.')
      if (!isEditable(record.status)) throw new ApiError(409, 'This strategy is not in a state that can be submitted.')
      if (!current) throw new ApiError(409, 'Add some content before submitting.')
      record.status = 'PendingApproval'
      pushAudit(state, 'StrategySubmittedEvent', { documentId: record.id, submittedByUserId: DEMO_USER_ID })
      commit()
      return { status: record.status }
    }

    case 'generate': {
      must(canEdit(role), 'Your role cannot request AI drafts.')
      if (!isEditable(record.status)) throw new ApiError(409, 'Drafts can only be requested while the strategy is editable.')
      const requests = (state.generations[record.id] ??= [])
      if (requests.some((r) => r.status === 'Pending')) throw new ApiError(409, 'A draft is already being generated.')

      const request: GenerationRequest = {
        id: uid(),
        status: 'Pending',
        attempts: 1,
        requestedAtUtc: new Date().toISOString(),
        completedAtUtc: null,
        error: null,
        model: null,
        promptVersion: null,
      }
      requests.unshift(request)
      state.aiUsed += 1
      pushAudit(state, 'StrategyGenerationRequestedEvent', { documentId: record.id, requestedByUserId: DEMO_USER_ID })
      commit()

      void runGeneration(record.id, request.id)
      return { requestId: request.id }
    }

    case 'approve':
    case 'reject': {
      must(role === 'Admin', 'Only Admins can review strategies.')
      if (record.status !== 'PendingApproval') throw new ApiError(409, 'This strategy is not waiting for approval.')
      if (!current || text(body.revisionId).toLowerCase() !== current.id.toLowerCase()) {
        throw new ApiError(409, 'The strategy changed after you opened it. Reload and review the current revision.')
      }

      const approved = action === 'approve'
      const reason = text(body.reason)
      if (!approved && !reason) throw new ApiError(400, 'A reason is required when sending a strategy back.')

      record.reviews.push({
        id: uid(),
        revisionId: current.id,
        reviewerUserId: DEMO_USER_ID,
        decision: approved ? 'Approved' : 'Rejected',
        reason: approved ? null : reason,
        reviewedAtUtc: new Date().toISOString(),
      })
      record.status = approved ? 'Approved' : 'NeedsRevision'
      pushAudit(
        state,
        approved ? 'StrategyApprovedEvent' : 'StrategyRejectedEvent',
        approved
          ? { documentId: record.id, reviewerId: DEMO_USER_ID }
          : { documentId: record.id, reviewerId: DEMO_USER_ID, reason },
      )
      commit()
      return { status: record.status }
    }

    case 'implemented': {
      must(role === 'Admin' || role === 'Editor', 'Your role cannot mark strategies as implemented.')
      if (record.status !== 'Approved') throw new ApiError(409, 'Only approved strategies can be marked as implemented.')
      record.status = 'Implemented'
      pushAudit(state, 'StrategyImplementedEvent', { documentId: record.id, implementedByUserId: DEMO_USER_ID })
      commit()
      return { status: record.status }
    }

    default:
      throw new ApiError(404, 'Not found.')
  }
}

function invitations(method: string, rest: string, body: Record<string, unknown>): unknown {
  if (method === 'GET' && rest === '/invitations') {
    return [...state.invitations].sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc))
  }

  if (method === 'POST' && rest === '/invitations') {
    const email = text(body.email).toLowerCase()
    const role = text(body.role)
    if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)) throw new ApiError(400, 'Enter a valid email address.')
    if (!['Admin', 'Strategist', 'Editor'].includes(role)) throw new ApiError(400, 'Choose a valid role.')
    if (state.invitations.some((i) => i.status === 'Pending' && i.email === email)) {
      throw new ApiError(409, 'There is already a pending invitation for that address.')
    }

    const invitation: Invitation = {
      id: uid(),
      email,
      role,
      status: 'Pending',
      createdAtUtc: new Date().toISOString(),
      expiresAtUtc: new Date(Date.now() + 7 * 86_400_000).toISOString(),
    }
    state.invitations.push(invitation)
    commit()
    return { invitationId: invitation.id }
  }

  const revoke = /^\/invitations\/([^/]+)\/revoke$/.exec(rest)
  if (method === 'POST' && revoke) {
    const invitation = state.invitations.find((i) => i.id === revoke[1])
    if (!invitation) throw new ApiError(404, 'Not found.')
    if (invitation.status !== 'Pending') throw new ApiError(409, 'Only pending invitations can be revoked.')
    invitation.status = 'Revoked'
    commit()
    return { status: invitation.status }
  }

  throw new ApiError(404, 'Not found.')
}
