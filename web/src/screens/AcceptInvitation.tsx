import { useNavigate, useParams } from 'react-router-dom'
import { useMsal } from '@azure/msal-react'
import { ApiError } from '../api/apiFetch'
import { useAcceptInvitation } from '../api/team'
import { rememberInvite } from '../auth/pendingInvite'

export function AcceptInvitation() {
  const { invitationId = '' } = useParams()
  const { instance, accounts } = useMsal()
  const navigate = useNavigate()
  const accept = useAcceptInvitation()

  const notFound = accept.error instanceof ApiError && accept.error.status === 404

  function useAnotherAccount() {
    rememberInvite(invitationId)
    instance.logoutRedirect()
  }

  return (
    <div className="rounded border bg-white p-6">
      <h2 className="text-lg font-semibold">You have been invited</h2>
      <p className="mt-2 text-slate-600">
        You are signed in as <strong>{accounts[0]?.username}</strong>. An invitation only works for the address it
        was sent to.
      </p>

      <button
        className="mt-4 rounded bg-indigo-600 px-4 py-2 font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
        disabled={accept.isPending}
        onClick={() =>
          accept.mutate(invitationId, {
            onSuccess: (result) => navigate(`/orgs/${result.organizationId}`),
          })
        }
      >
        {accept.isPending ? 'Joining…' : 'Accept invitation'}
      </button>

      {accept.isError && (
        <p className="mt-3 rounded border border-red-300 bg-red-50 p-3 text-sm text-red-800">
          {notFound
            ? 'This invitation was not found for your account. It may have been revoked, or it was sent to a different address.'
            : accept.error.message}
        </p>
      )}

      <button className="mt-4 block text-sm text-indigo-600 hover:underline" onClick={useAnotherAccount}>
        Sign in with a different account
      </button>
    </div>
  )
}