import { useState } from 'react'
import { useCreateInvitation, useInvitations, useRevokeInvitation, type Invitation } from '../api/team'
import { canManageTeam } from './roles'
import { useTenant } from './useTenant'

const roleHelp: Record<string, string> = {
  Admin: 'Connects channels, invites people, approves or rejects strategies.',
  Strategist: 'Requests AI drafts, edits and submits strategies for approval.',
  Editor: 'Sees approved strategies and marks them implemented.',
}

const statusStyles: Record<Invitation['status'], string> = {
  Pending: 'bg-amber-100 text-amber-800',
  Accepted: 'bg-green-100 text-green-800',
  Revoked: 'bg-slate-100 text-slate-600',
  Expired: 'bg-slate-100 text-slate-600',
}

function InviteForm({ tenantId }: { tenantId: string }) {
  const create = useCreateInvitation(tenantId)
  const [email, setEmail] = useState('')
  const [role, setRole] = useState('Strategist')

  return (
    <form
      className="rounded border bg-white p-4"
      onSubmit={(event) => {
        event.preventDefault()
        create.mutate({ email: email.trim(), role }, { onSuccess: () => setEmail('') })
      }}
    >
      <h3 className="text-lg font-semibold">Invite someone</h3>
      <p className="text-sm text-slate-600">
        No email is sent. After creating the invitation, copy its link and send it to the person yourself. The
        invitation only works when they sign in with this exact address, and it expires after 7 days.
      </p>

      <div className="mt-3 flex flex-wrap gap-2">
        <input
          type="email"
          className="min-w-56 flex-1 rounded border px-3 py-2 text-sm"
          placeholder="Their sign-in address"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
        />
        <select
          className="rounded border px-3 py-2 text-sm"
          value={role}
          onChange={(event) => setRole(event.target.value)}
          aria-label="Role"
        >
          <option value="Admin">Admin</option>
          <option value="Strategist">Strategist</option>
          <option value="Editor">Editor</option>
        </select>
        <button
          type="submit"
          disabled={create.isPending || !email.trim()}
          className="rounded bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
        >
          {create.isPending ? 'Creating…' : 'Create invitation'}
        </button>
      </div>

      <p className="mt-2 text-xs text-slate-500">{roleHelp[role]}</p>
      {create.isError && <p className="mt-2 text-sm text-red-700">{create.error.message}</p>}
    </form>
  )
}

function InvitationRow({ invitation, tenantId }: { invitation: Invitation; tenantId: string }) {
  const revoke = useRevokeInvitation(tenantId)
  const [copied, setCopied] = useState(false)
  const link = `${window.location.origin}/invite/${invitation.id}`
  const pending = invitation.status === 'Pending'

  async function copy() {
    try {
      await navigator.clipboard.writeText(link)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      // Clipboard blocked: the link is shown below, so it can be copied by hand.
    }
  }

  return (
    <li className="rounded border bg-white p-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <p className="font-medium">{invitation.email}</p>
          <p className="text-xs text-slate-500">
            {invitation.role} · created {new Date(invitation.createdAtUtc).toLocaleDateString()} · expires{' '}
            {new Date(invitation.expiresAtUtc).toLocaleDateString()}
          </p>
        </div>
        <span className={`rounded px-2 py-0.5 text-xs font-medium ${statusStyles[invitation.status]}`}>
          {invitation.status}
        </span>
      </div>

      {pending && (
        <div className="mt-3 flex flex-wrap items-center gap-2">
          <input
            readOnly
            value={link}
            onFocus={(event) => event.currentTarget.select()}
            className="min-w-56 flex-1 rounded border bg-slate-50 px-2 py-1 text-xs"
            aria-label="Invitation link"
          />
          <button className="rounded border px-3 py-1 text-sm hover:bg-slate-100" onClick={copy}>
            {copied ? 'Copied' : 'Copy link'}
          </button>
          <button
            className="rounded border border-red-300 px-3 py-1 text-sm text-red-800 hover:bg-red-50 disabled:opacity-50"
            disabled={revoke.isPending}
            onClick={() => revoke.mutate(invitation.id)}
          >
            {revoke.isPending ? 'Revoking…' : 'Revoke'}
          </button>
        </div>
      )}

      {revoke.isError && <p className="mt-2 text-sm text-red-700">{revoke.error.message}</p>}
    </li>
  )
}

function TeamAdmin({ tenantId }: { tenantId: string }) {
  const invitations = useInvitations(tenantId)

  return (
    <div className="space-y-6">
      <InviteForm tenantId={tenantId} />

      <section>
        <h3 className="text-lg font-semibold">Invitations</h3>

        {invitations.isPending && <p className="mt-2 text-slate-500">Loading invitations…</p>}
        {invitations.isError && <p className="mt-2 text-red-700">{invitations.error.message}</p>}

        {invitations.data && invitations.data.length === 0 && (
          <p className="mt-2 text-slate-600">No invitations yet.</p>
        )}

        {invitations.data && invitations.data.length > 0 && (
          <ul className="mt-3 space-y-2">
            {invitations.data.map((invitation) => (
              <InvitationRow key={invitation.id} invitation={invitation} tenantId={tenantId} />
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}

export function Team() {
  const { tenantId, role } = useTenant()

  if (!canManageTeam(role)) {
    return <p className="rounded border bg-white p-4 text-slate-600">Only Admins can manage the team.</p>
  }

  return <TeamAdmin tenantId={tenantId} />
}