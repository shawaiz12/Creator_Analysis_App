import { useEffect } from 'react'
import { matchPath, useLocation, useNavigate } from 'react-router-dom'
import { rememberInvite, takePendingInvite } from './pendingInvite'

// Shown to signed-out visitors: if they arrived through an invitation link, remember it.
export function RememberInvite() {
  const { pathname } = useLocation()
  const invitationId = matchPath('/invite/:invitationId', pathname)?.params.invitationId

  useEffect(() => {
    if (invitationId) rememberInvite(invitationId)
  }, [invitationId])

  if (!invitationId) return null

  return (
    <p className="mb-4 rounded border border-indigo-200 bg-indigo-50 p-3 text-sm text-indigo-900">
      You were invited to join an organization. Sign in with the address the invitation was sent to, and you will
      come straight back to it.
    </p>
  )
}

// Shown to signed-in users: after returning from Microsoft, continue to the invitation they came for.
export function PendingInviteRedirect() {
  const navigate = useNavigate()

  useEffect(() => {
    const invitationId = takePendingInvite()
    if (invitationId) navigate(`/invite/${invitationId}`, { replace: true })
  }, [navigate])

  return null
}