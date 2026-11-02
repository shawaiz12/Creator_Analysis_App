import type { ReactNode } from 'react'
import { AuthenticatedTemplate, UnauthenticatedTemplate, useMsal } from '@azure/msal-react'
import { Link, Route, Routes, useNavigate } from 'react-router-dom'
import { apiScopes } from './auth/msal'
import { PendingInviteRedirect, RememberInvite } from './auth/InviteRedirects'
import { Wordmark } from './brand/Logo'
import { DemoBanner } from './demo/DemoBanner'
import { DEMO_TENANT_ID, DEMO_USER_EMAIL, enterDemo, useIsDemo } from './demo/demoMode'
import { Home } from './screens/Home'
import { Landing } from './screens/Landing'
import { NotFound } from './screens/NotFound'
import { AcceptInvitation } from './screens/AcceptInvitation'
import { ThemeToggle } from './theme/ThemeToggle'
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

function Page({ children }: { children: ReactNode }) {
  return <div className="mx-auto max-w-6xl px-5 py-6">{children}</div>
}

export default function App() {
  const { instance, accounts } = useMsal()
  const isDemo = useIsDemo()
  const navigate = useNavigate()

  const signIn = () => instance.loginRedirect({ scopes: apiScopes })
  const continueAsGuest = () => {
    enterDemo()
    navigate(`/orgs/${DEMO_TENANT_ID}`)
  }

  return (
    <div className="min-h-screen bg-canvas text-ink">
      {isDemo && <DemoBanner />}

      <header className="sticky top-0 z-20 border-b bg-canvas/85 backdrop-blur">
        <div className="mx-auto flex h-14 max-w-6xl items-center justify-between px-5">
          <Link to="/" aria-label="Content Dock home">
            <Wordmark />
          </Link>

          <div className="flex items-center gap-2 text-sm">
            {isDemo ? (
              <span className="hidden text-subtle sm:inline">{DEMO_USER_EMAIL}</span>
            ) : (
              <AuthenticatedTemplate>
                <span className="hidden text-subtle sm:inline">{accounts[0]?.username}</span>
                <button
                  className="rounded-lg border px-3 py-1.5 hover:bg-raised"
                  onClick={() => instance.logoutRedirect()}
                >
                  Sign out
                </button>
              </AuthenticatedTemplate>
            )}
            <ThemeToggle />
          </div>
        </div>
      </header>

      {isDemo ? (
        <Page>
          <AppRoutes />
        </Page>
      ) : (
        <>
          <UnauthenticatedTemplate>
            <RememberInvite />
            <Landing onSignIn={signIn} onGuest={continueAsGuest} />
          </UnauthenticatedTemplate>

          <AuthenticatedTemplate>
            <Page>
              <PendingInviteRedirect />
              <AppRoutes />
            </Page>
          </AuthenticatedTemplate>
        </>
      )}
    </div>
  )
}
