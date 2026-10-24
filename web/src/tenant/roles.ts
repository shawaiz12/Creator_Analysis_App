// Mirrors the API's rules for channels, videos and retention. Cosmetic only: the API enforces it.
export const canViewAnalytics = (role: string) => role === 'Admin' || role === 'Strategist'
export const canConnectChannel = (role: string) => role === 'Admin'