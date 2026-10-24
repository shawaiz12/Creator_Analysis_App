import { InteractionRequiredAuthError } from '@azure/msal-browser'
import { apiScopes, msalInstance } from '../auth/msal'

export class SignInRequiredError extends Error {}

export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

async function getAccessToken(): Promise<string> {
  const account = msalInstance.getActiveAccount() ?? msalInstance.getAllAccounts()[0]
  if (!account) throw new SignInRequiredError('You are not signed in.')

  try {
    const result = await msalInstance.acquireTokenSilent({ scopes: apiScopes, account })
    return result.accessToken
  } catch (error) {
    // Silent renewal failed (for example after the roughly one-day limit on browser sessions).
    // We do not pop up a sign-in from here; the screen decides how to ask the user.
    if (error instanceof InteractionRequiredAuthError) {
      throw new SignInRequiredError('Your session has expired. Please sign in again.')
    }
    throw error
  }
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = await getAccessToken()

  const headers = new Headers(init.headers)
  headers.set('Authorization', `Bearer ${token}`)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')

  const response = await fetch(`${import.meta.env.VITE_API_BASE_URL}${path}`, { ...init, headers })

  if (!response.ok) {
    let message = `The request failed (${response.status}).`
    try {
      const body = await response.json()
      if (typeof body?.error === 'string') message = body.error
    } catch {
      // The error had no JSON body; keep the generic message.
    }
    throw new ApiError(response.status, message)
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}