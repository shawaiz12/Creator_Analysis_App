import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMsal } from '@azure/msal-react'
import { apiScopes } from '../auth/msal'
import { SignInRequiredError } from '../api/apiFetch'
import { useCreateOrganization, useMe } from '../api/me'

function CreateOrganizationForm() {
  const [name, setName] = useState('')
  const create = useCreateOrganization()
  const navigate = useNavigate()

  return (
    <form
      className="mt-6 rounded border bg-surface p-4"
      onSubmit={(event) => {
        event.preventDefault()
        create.mutate(name.trim(), {
          onSuccess: (result) => {
            setName('')
            navigate(`/orgs/${result.organizationId}`)
          },
        })
      }}
    >
      <label htmlFor="org-name" className="block text-sm font-medium">
        Create a new organization
      </label>
      <div className="mt-2 flex gap-2">
        <input
          id="org-name"
          className="flex-1 rounded border px-3 py-2"
          placeholder="Acme Studio"
          value={name}
          onChange={(event) => setName(event.target.value)}
        />
        <button
          type="submit"
          disabled={create.isPending || name.trim().length === 0}
          className="rounded bg-primary px-4 py-2 font-medium text-on-solid hover:bg-primary-hover disabled:opacity-50"
        >
          {create.isPending ? 'Creating…' : 'Create'}
        </button>
      </div>
      {create.isError && <p className="mt-2 text-sm text-bad">{create.error.message}</p>}
    </form>
  )
}

export function Home() {
  const { instance } = useMsal()
  const me = useMe()

  if (me.isPending) return <p className="text-subtle">Loading your organizations…</p>

  if (me.isError) {
    if (me.error instanceof SignInRequiredError) {
      return (
        <div className="rounded border border-pending/40 bg-pending-soft p-4">
          <p>{me.error.message}</p>
          <button
            className="mt-3 rounded bg-primary px-4 py-2 font-medium text-on-solid hover:bg-primary-hover"
            onClick={() => instance.loginRedirect({ scopes: apiScopes })}
          >
            Sign in again
          </button>
        </div>
      )
    }
    return (
      <p className="rounded border border-bad/40 bg-bad-soft p-4 text-bad">
        Could not load your profile: {me.error.message}
      </p>
    )
  }

  const organizations = me.data.organizations

  return (
    <section>
      <h2 className="text-lg font-semibold">Your organizations</h2>

      {organizations.length === 0 ? (
        <p className="mt-2 text-muted">You are not in an organization yet. Create your first one below.</p>
      ) : (
        <ul className="mt-3 space-y-2">
          {organizations.map((organization) => (
            <li key={organization.organizationId}>
              <Link
                to={`/orgs/${organization.organizationId}`}
                className="flex items-center justify-between rounded border bg-surface p-3 hover:border-line-strong"
              >
                <span className="font-medium">{organization.organizationName}</span>
                <span className="rounded bg-raised px-2 py-0.5 text-xs">{organization.role}</span>
              </Link>
            </li>
          ))}
        </ul>
      )}

      <CreateOrganizationForm />
    </section>
  )
}