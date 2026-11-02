// A retention curve falling into a berth, with a lamp at the end: a draft waiting at the dock.
export function LogoMark({ className = 'size-8' }: { className?: string }) {
  return (
    <svg viewBox="0 0 32 32" className={className} aria-hidden="true">
      <rect width="32" height="32" rx="8" fill="var(--ink)" />
      <path
        d="M7 9.5 C 13 9.5, 14.5 21, 20.5 21 L 26 21"
        fill="none"
        stroke="var(--canvas)"
        strokeWidth="2.6"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle cx="20.5" cy="21" r="3.1" fill="var(--pending)" />
    </svg>
  )
}

export function Wordmark() {
  return (
    <span className="flex items-center gap-2.5">
      <LogoMark />
      <span className="font-display text-xl font-semibold tracking-tight">Content Dock</span>
    </span>
  )
}
