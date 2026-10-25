const key = 'pendingInvitation'
const looksLikeGuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export function rememberInvite(invitationId: string) {
  try {
    sessionStorage.setItem(key, invitationId)
  } catch {
    // Storage unavailable: the person can simply open the link again after signing in.
  }
}

// Returns the remembered invitation (once) and forgets it. Anything that is not a GUID is ignored.
export function takePendingInvite(): string | null {
  try {
    const value = sessionStorage.getItem(key)
    sessionStorage.removeItem(key)
    return value && looksLikeGuid.test(value) ? value : null
  } catch {
    return null
  }
}