import { useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { meKey } from '../api/me'
import { DEMO_ROLES, type DemoRole, exitDemo, setDemoRole, useDemoRole } from './demoMode'

export function DemoBanner() {
  const role = useDemoRole()
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return (
    <div className="border-b border-amber-300 bg-amber-50 text-sm text-amber-900">
      <div className="mx-auto flex max-w-3xl flex-wrap items-center gap-x-4 gap-y-2 p-3">
        <span className="font-semibold">Demo mode</span>
        <span>Sample data only. Nothing you do here leaves your browser.</span>

        <label className="ml-auto flex items-center gap-2">
          View as
          <select
            className="rounded border border-amber-400 bg-white px-2 py-1"
            value={role}
            onChange={(event) => {
              setDemoRole(event.target.value as DemoRole)
              queryClient.invalidateQueries({ queryKey: meKey })
            }}
          >
            {DEMO_ROLES.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>

        <button
          className="rounded border border-amber-400 bg-white px-3 py-1 hover:bg-amber-100"
          onClick={() => {
            exitDemo()
            queryClient.clear()
            navigate('/')
          }}
        >
          Exit demo
        </button>
      </div>
    </div>
  )
}