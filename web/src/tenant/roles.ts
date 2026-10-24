// These mirror the API's rules. They only decide what to show; the API enforces them on every request.
export const canViewAnalytics = (role: string) => role === 'Admin' || role === 'Strategist'
export const canConnectChannel = (role: string) => role === 'Admin'

export const canEditStrategy = (role: string) => role === 'Admin' || role === 'Strategist'
export const canReview = (role: string) => role === 'Admin'
export const canMarkImplemented = (role: string) => role === 'Admin' || role === 'Editor'