import { Link } from 'react-router-dom'

export function NotFound() {
  return (
    <div className="rounded border bg-surface p-6">
      <h2 className="text-lg font-semibold">Nothing here</h2>
      <p className="mt-2 text-muted">This page does not exist, or you do not have access to it.</p>
      <Link to="/" className="mt-4 inline-block text-link hover:underline">
        Back to your organizations
      </Link>
    </div>
  )
}