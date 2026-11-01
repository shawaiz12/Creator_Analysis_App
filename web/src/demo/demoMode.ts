import { useSyncExternalStore } from 'react'

const FLAG_KEY = 'contentdock.demo'
const ROLE_KEY = 'contentdock.demo.role'

export const DEMO_TENANT_ID = '0d3a1f6e-8c44-4b6a-9a51-3f7c2b9d1e10'
export const DEMO_USER_ID = '0d3a1f6e-8c44-4b6a-9a51-3f7c2b9d1e11'
export const DEMO_USER_EMAIL = 'guest@contentdock.demo'

export type DemoRole = 'Admin' | 'Strategist' | 'Editor'
export const DEMO_ROLES: DemoRole[] = ['Admin', 'Strategist', 'Editor']

// sessionStorage can be blocked (some private windows), so keep an in-memory copy too.
let memoryFlag = false
let memoryRole: DemoRole = 'Admin'

const listeners = new Set<() => void>()
const notify = () => listeners.forEach((listener) => listener())

function read(key: string): string | null {
  try {
    return sessionStorage.getItem(key)
  } catch {
    return null
  }
}

function write(key: string, value: string | null) {
  try {
    if (value === null) sessionStorage.removeItem(key)
    else sessionStorage.setItem(key, value)
  } catch {
    // The in-memory copy still works for this visit.
  }
}

export function isDemoMode(): boolean {
  return memoryFlag || read(FLAG_KEY) === '1'
}

export function enterDemo() {
  memoryFlag = true
  write(FLAG_KEY, '1')
  notify()
}

export function exitDemo() {
  memoryFlag = false
  memoryRole = 'Admin'
  write(FLAG_KEY, null)
  write(ROLE_KEY, null)
  notify()
}

export function getDemoRole(): DemoRole {
  const stored = read(ROLE_KEY)
  return DEMO_ROLES.find((role) => role === stored) ?? memoryRole
}

export function setDemoRole(role: DemoRole) {
  memoryRole = role
  write(ROLE_KEY, role)
  notify()
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

export const useIsDemo = () => useSyncExternalStore(subscribe, isDemoMode)
export const useDemoRole = () => useSyncExternalStore(subscribe, getDemoRole)
