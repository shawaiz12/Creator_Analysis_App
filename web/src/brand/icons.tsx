import type { ReactNode } from 'react'

function Icon({ children }: { children: ReactNode }) {
  return (
    <svg
      viewBox="0 0 24 24"
      className="size-5 shrink-0"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      {children}
    </svg>
  )
}

export const OverviewIcon = () => (
  <Icon>
    <rect x="3.5" y="3.5" width="7" height="7" rx="1.5" />
    <rect x="13.5" y="3.5" width="7" height="7" rx="1.5" />
    <rect x="3.5" y="13.5" width="7" height="7" rx="1.5" />
    <rect x="13.5" y="13.5" width="7" height="7" rx="1.5" />
  </Icon>
)

export const VideosIcon = () => (
  <Icon>
    <rect x="3" y="5" width="18" height="14" rx="3" />
    <path d="m10.5 9.5 4 2.5-4 2.5Z" />
  </Icon>
)

export const StrategiesIcon = () => (
  <Icon>
    <path d="M6 3.5h8.5L19 8v12.5H6Z" />
    <path d="M14 3.5V8.5h5M9.5 13.5l1.8 1.8 3.4-3.6" />
  </Icon>
)

export const TeamIcon = () => (
  <Icon>
    <circle cx="9" cy="8.5" r="3.2" />
    <path d="M3 19.5c.6-3.3 3-5 6-5s5.4 1.7 6 5" />
    <path d="M16 5.7a3.2 3.2 0 0 1 0 5.8M17.5 14.8c2 .6 3.1 2.3 3.5 4.7" />
  </Icon>
)

export const AuditIcon = () => (
  <Icon>
    <path d="M4 5.5h16M4 12h16M4 18.5h9" />
    <circle cx="18" cy="18" r="2.2" />
  </Icon>
)
