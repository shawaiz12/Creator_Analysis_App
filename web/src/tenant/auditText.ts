import type { AuditEntry } from '../api/audit'

const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

// Audit data is stored exactly as the event was serialized (PascalCase), so look names up ignoring case.
function field(data: unknown, name: string): unknown {
  if (data === null || typeof data !== 'object') return undefined

  const wanted = name.toLowerCase()
  for (const [key, value] of Object.entries(data as Record<string, unknown>)) {
    if (key.toLowerCase() === wanted) return value
  }
  return undefined
}

const text = (value: unknown) => (typeof value === 'string' && value.length > 0 ? value : undefined)

export function describeEntry(entry: AuditEntry): string {
  switch (entry.eventType) {
    case 'StrategyGenerationRequestedEvent':
      return 'AI draft requested'

    case 'StrategyRevisionAddedEvent': {
      const version = field(entry.data, 'versionNumber')
      const label = typeof version === 'number' ? `Revision v${version}` : 'Revision'
      const origin = field(entry.data, 'origin') === 'Ai' ? 'AI draft' : 'human edit'
      return `${label} added (${origin})`
    }

    case 'StrategySubmittedEvent':
      return 'Submitted for approval'

    case 'StrategyApprovedEvent':
      return 'Approved'

    case 'StrategyRejectedEvent': {
      const reason = text(field(entry.data, 'reason'))
      return reason ? `Rejected: ${reason}` : 'Rejected'
    }

    case 'StrategyImplementedEvent':
      return 'Marked as implemented'

    default:
      return entry.eventType
  }
}

// Who did it. Only ids are recorded, so until a members list exists we show a short id.
export function actorOf(entry: AuditEntry): string {
  if (entry.eventType === 'StrategyRevisionAddedEvent' && field(entry.data, 'origin') === 'Ai') return 'AI'

  const names = ['reviewerId', 'submittedByUserId', 'implementedByUserId', 'requestedByUserId', 'authorUserId']
  for (const name of names) {
    const id = text(field(entry.data, name))
    if (id) return `User ${id.slice(0, 8)}`
  }
  return 'System'
}

export function documentIdOf(entry: AuditEntry): string | undefined {
  const id = text(field(entry.data, 'documentId'))
  return id && guid.test(id) ? id : undefined
}