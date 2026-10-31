import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react'
import { Link, Route, Routes, useNavigate } from 'react-router-dom'
import { apiScopes } from './auth/msal'
import { PendingInviteRedirect, RememberInvite } from './auth/InviteRedirects'
import { DemoBanner } from './demo/DemoBanner'
import { DEMO_TENANT_ID, DEMO_USER_EMAIL, enterDemo, useIsDemo } from './demo/demoMode'
import { Home } from './screens/Home'
import { NotFound } from './screens/NotFound'
import { AcceptInvitation } from './screens/AcceptInvitation'
import { TenantLayout } from './tenant/TenantLayout'
import { Overview } from './tenant/Overview'
import { AnalyticsOnly } from './tenant/AnalyticsOnly'
import { Videos } from './tenant/Videos'
import { VideoRetention } from './tenant/VideoRetention'
import { Strategies } from './tenant/Strategies'
import { StrategyDetail } from './tenant/StrategyDetail'
import { Team } from './tenant/Team'
import { Audit } from './tenant/Audit'

function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
      <Route path="/invite/:invitationId" element={<AcceptInvitation />} />
      <Route path="/orgs/:tenantId" element={<TenantLayout />}>
        <Route index element={<Overview />} />
        <Route path="strategies" element={<Strategies />} />
        <Route path="strategies/:strategyId" element={<StrategyDetail />} />
        <Route path="team" element={<Team />} />
        <Route path="audit" element={<Audit />} />
        <Route element={<AnalyticsOnly />}>
          <Route path="videos" element={<Videos />} />
          <Route path="videos/:videoId" element={<VideoRetention />} />
        </Route>
      </Route>
      <Route path="*" element={<NotFound />} />
    </Routes>
  )
}

export default function App() {
  const { instance, accounts } = useMsal()
  const isDemo = useIsDemo()
  const navigate = useNavigate()

  return (
    <main className="min-h-screen bg-slate-50 text-slate-900">
      {isDemo && <DemoBanner />}

      <header className="border-b bg-white">
        <div className="mx-auto flex max-w-3xl items-center justify-between p-4">
          <Link to="/" className="text-xl font-bold">
            Content Dock
          </Link>
          {isDemo ? (
            <span className="text-sm text-slate-600">{DEMO_USER_EMAIL}</span>
          ) : (
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
          )}
        </div>
      </header>

      <div className="mx-auto max-w-3xl p-6">
        {isDemo ? (
          <AppRoutes />
        ) : (
          <>
            <UnauthenticatedTemplate>
              <RememberInvite />
              <p className="text-slate-600">Sign in with your Microsoft account to continue.</p>
              <div className="mt-4 flex flex-wrap items-center gap-3">
                <button
                  className="rounded bg-indigo-600 px-4 py-2 font-medium text-white hover:bg-indigo-700"
                  onClick={() => instance.loginRedirect({ scopes: apiScopes })}
                >
                  Sign in with Microsoft
                </button>
                <button
                  className="rounded border border-indigo-600 px-4 py-2 font-medium text-indigo-700 hover:bg-indigo-50"
                  onClick={() => {
                    enterDemo()
                    navigate(`/orgs/${DEMO_TENANT_ID}`)
                  }}
                >
                  Try the demo
                </button>
              </div>
              <p className="mt-2 text-sm text-slate-500">No account needed. Explore with sample data.</p>
            </UnauthenticatedTemplate>

            <AuthenticatedTemplate>
              <PendingInviteRedirect />
              <AppRoutes />
            </AuthenticatedTemplate>
          </>
        )}
      </div>
    </main>
  )
}