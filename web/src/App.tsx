import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react'
import { Link, Route, Routes } from 'react-router-dom'
import { apiScopes } from './auth/msal'
import { Home } from './screens/Home'
import { NotFound } from './screens/NotFound'
import { TenantLayout } from './tenant/TenantLayout'
import { Overview } from './tenant/Overview'
import { AnalyticsOnly } from './tenant/AnalyticsOnly'
import { Videos } from './tenant/Videos'
import { VideoRetention } from './tenant/VideoRetention'

export default function App() {
  const { instance, accounts } = useMsal()

  return (
    <main className="min-h-screen bg-slate-50 text-slate-900">
      <header className="border-b bg-white">
        <div className="mx-auto flex max-w-3xl items-center justify-between p-4">
          <Link to="/" className="text-xl font-bold">
            CreatorAnalytics
          </Link>
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
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/orgs/:tenantId" element={<TenantLayout />}>
              <Route index element={<Overview />} />
              <Route element={<AnalyticsOnly />}>
                <Route path="videos" element={<Videos />} />
                <Route path="videos/:videoId" element={<VideoRetention />} />
              </Route>
            </Route>
            <Route path="*" element={<NotFound />} />
          </Routes>
        </AuthenticatedTemplate>
      </div>
    </main>
  )
}