import { useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { meKey } from '../api/me'
import { resetDemoState } from './demoApi'
import { DEMO_ROLES, DEMO_TENANT_ID, type DemoRole, exitDemo, setDemoRole, useDemoRole } from './demoMode'

const buttonClass = 'rounded-md border border-pending/40 px-2.5 py-1 hover:bg-pending-soft'

export function DemoBanner() {
  const role = useDemoRole()
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return (
    <div className="border-b border-pending/30 bg-pending-soft text-sm text-pending">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-4 gap-y-2 px-5 py-2">
        <span className="font-semibold">Guest demo</span>
        <span className="text-ink/80">Sample data. Your changes stay in this browser tab.</span>

        <div className="ml-auto flex flex-wrap items-center gap-2">
          <label className="flex items-center gap-2">
            View as
            <select
              className="rounded-md border border-pending/40 px-2 py-1 text-ink"
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
            className={buttonClass}
            onClick={() => {
              resetDemoState()
              queryClient.clear()
              navigate(`/orgs/${DEMO_TENANT_ID}`)
            }}
          >
            Reset demo
          </button>

          <button
            className={buttonClass}
            onClick={() => {
              resetDemoState()
              exitDemo()
              queryClient.clear()
              navigate('/')
            }}
          >
            Exit demo
          </button>
        </div>
      </div>
    </div>
  )
}
