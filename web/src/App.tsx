import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react'
import { apiScopes } from './auth/msal'
import { Home } from './screens/Home'

export default function App() {
  const { instance, accounts } = useMsal()

  return (
    <main className="min-h-screen bg-slate-50 text-slate-900">
      <header className="border-b bg-white">
        <div className="mx-auto flex max-w-3xl items-center justify-between p-4">
          <h1 className="text-xl font-bold">CreatorAnalytics</h1>
          <AuthenticatedTemplate>
            <div className="flex items-center gap-3 text-sm">
              <span className="text-slate-600">{accounts[0]?.username}</span>
              <button
                className="rounded border px-3 py-1 hover:bg-slate-100"
                onClick={() => instance.logoutRedirect()}
              >
                Sign out
              </button>
            </div>
          </AuthenticatedTemplate>
        </div>
      </header>

      <div className="mx-auto max-w-3xl p-6">
        <UnauthenticatedTemplate>
          <p className="text-slate-600">Sign in with your Microsoft account to continue.</p>
          <button
            className="mt-4 rounded bg-indigo-600 px-4 py-2 font-medium text-white hover:bg-indigo-700"
            onClick={() => instance.loginRedirect({ scopes: apiScopes })}
          >
            Sign in with Microsoft
          </button>
        </UnauthenticatedTemplate>

        <AuthenticatedTemplate>
          <Home />
        </AuthenticatedTemplate>
      </div>
    </main>
  )
}