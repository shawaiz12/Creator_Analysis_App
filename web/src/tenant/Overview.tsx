import { useTenant } from './useTenant'

export function Overview() {
  const { name, role } = useTenant()

  return (
    <div className="rounded border bg-white p-4">
      <p>
        You are working inside <strong>{name}</strong> as <strong>{role}</strong>.
      </p>
      <p className="mt-2 text-sm text-slate-600">Channels, videos and strategies will appear here next.</p>
    </div>
  )
}