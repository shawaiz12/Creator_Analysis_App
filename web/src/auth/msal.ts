import { PublicClientApplication, type Configuration } from '@azure/msal-browser'

const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_WEB_CLIENT_ID,
    authority: `https://login.microsoftonline.com/${import.meta.env.VITE_TENANT_ID}`,
    redirectUri: window.location.origin,
  },
  cache: { cacheLocation: 'sessionStorage' },
}

// The permission we ask Microsoft for: "call the CreatorAnalytics API as this user".
export const apiScopes = [`api://${import.meta.env.VITE_API_CLIENT_ID}/access_as_user`]

// One instance for the whole app (the docs insist on exactly one).
export const msalInstance = new PublicClientApplication(msalConfig)