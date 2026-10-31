import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { EventType, type AuthenticationResult } from '@azure/msal-browser'
import { MsalProvider } from '@azure/msal-react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import './index.css'
import App from './App'
import { msalInstance } from './auth/msal'
import { DEMO_TENANT_ID, enterDemo } from './demo/demoMode'

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, refetchOnWindowFocus: false } },
})

async function start() {
  await msalInstance.initialize()

  const accounts = msalInstance.getAllAccounts()
  if (!msalInstance.getActiveAccount() && accounts.length > 0) {
    msalInstance.setActiveAccount(accounts[0])
  }
  msalInstance.addEventCallback((event) => {
    if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
      msalInstance.setActiveAccount((event.payload as AuthenticationResult).account)
    }
  })
  // An address ending in ?demo=1 opens guest mode straight away.
  if (new URLSearchParams(window.location.search).get('demo') === '1') {
    enterDemo()
    const target = window.location.pathname === '/' ? `/orgs/${DEMO_TENANT_ID}` : window.location.pathname
    window.history.replaceState({}, '', target)
  }
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <MsalProvider instance={msalInstance}>
        <QueryClientProvider client={queryClient}>
          <BrowserRouter>
            <App />
          </BrowserRouter>
        </QueryClientProvider>
      </MsalProvider>
    </StrictMode>,
  )
}

start()