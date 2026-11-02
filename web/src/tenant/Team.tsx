import { useState } from 'react'
import { useMe } from '../api/me'
import {
  useCreateInvitation,
  useInvitations,
  useMembers,
  useRevokeInvitation,
  type Invitation,
} from '../api/team'
import { canManageTeam } from './roles'
import { useTenant } from './useTenant'

const roleHelp: Record<string, string> = {
  Admin: 'Connects channels, invites people, approves or rejects strategies.',
  Strategist: 'Requests AI drafts, edits and submits strategies for approval.',
  Editor: 'Sees approved strategies and marks them implemented.',
}

const statusStyles: Record<Invitation['status'], string> = {
  Pending: 'bg-pending-soft text-pending',
  Accepted: 'bg-ok-soft text-ok',
  Revoked: 'bg-raised text-muted',
  Expired: 'bg-raised text-muted',
}

function InviteForm({ tenantId }: { tenantId: string }) {
  const create = useCreateInvitation(tenantId)
  const [email, setEmail] = useState('')
  const [role, setRole] = useState('Strategist')

  return (
    <form
      className="rounded border bg-surface p-4"
      onSubmit={(event) => {
        event.preventDefault()
        create.mutate({ email: email.trim(), role }, { onSuccess: () => setEmail('') })
      }}
    >
      <h3 className="text-lg font-semibold">Invite someone</h3>
      <p className="text-sm text-muted">
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
          className="rounded bg-primary px-4 py-2 text-sm font-medium text-on-solid hover:bg-primary-hover disabled:opacity-50"
        >
          {create.isPending ? 'Creating…' : 'Create invitation'}
        </button>
      </div>

      <p className="mt-2 text-xs text-subtle">{roleHelp[role]}</p>
      {create.isError && <p className="mt-2 text-sm text-bad">{create.error.message}</p>}
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
    <li className="rounded border bg-surface p-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <p className="font-medium">{invitation.email}</p>
          <p className="text-xs text-subtle">
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
            className="min-w-56 flex-1 rounded border bg-canvas px-2 py-1 text-xs"
            aria-label="Invitation link"
          />
          <button className="rounded border px-3 py-1 text-sm hover:bg-raised" onClick={copy}>
            {copied ? 'Copied' : 'Copy link'}
          </button>
          <button
            className="rounded border border-bad/40 px-3 py-1 text-sm text-bad hover:bg-bad-soft disabled:opacity-50"
            disabled={revoke.isPending}
            onClick={() => revoke.mutate(invitation.id)}
          >
            {revoke.isPending ? 'Revoking…' : 'Revoke'}
          </button>
        </div>
      )}

      {revoke.isError && <p className="mt-2 text-sm text-bad">{revoke.error.message}</p>}
    </li>
  )
}

function MemberList({ tenantId }: { tenantId: string }) {
  const members = useMembers(tenantId)
  const me = useMe()
  const myEmail = me.data?.email?.toLowerCase()

  return (
    <section>
      <h3 className="text-lg font-semibold">Members</h3>

      {members.isPending && <p className="mt-2 text-subtle">Loading members…</p>}
      {members.isError && <p className="mt-2 text-bad">{members.error.message}</p>}

      {members.data && (
        <ul className="mt-3 space-y-2">
          {members.data.map((member) => (
            <li key={member.userId} className="flex items-center justify-between rounded border bg-surface p-3">
              <span className="font-medium">
                {member.email}
                {member.email.toLowerCase() === myEmail && (
                  <span className="ml-2 text-xs font-normal text-subtle">(you)</span>
                )}
              </span>
              <span className="rounded bg-raised px-2 py-0.5 text-xs">{member.role}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

function TeamAdmin({ tenantId }: { tenantId: string }) {
  const invitations = useInvitations(tenantId)

  return (
    <div className="space-y-6">
      <MemberList tenantId={tenantId} />
      <InviteForm tenantId={tenantId} />

      <section>
        <h3 className="text-lg font-semibold">Invitations</h3>

        {invitations.isPending && <p className="mt-2 text-subtle">Loading invitations…</p>}
        {invitations.isError && <p className="mt-2 text-bad">{invitations.error.message}</p>}

        {invitations.data && invitations.data.length === 0 && (
          <p className="mt-2 text-muted">No invitations yet.</p>
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
    return <p className="rounded border bg-surface p-4 text-muted">Only Admins can manage the team.</p>
  }

  return <TeamAdmin tenantId={tenantId} />
}