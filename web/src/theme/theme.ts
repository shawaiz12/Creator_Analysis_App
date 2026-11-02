import { useSyncExternalStore } from 'react'

export type Theme = 'dark' | 'light'

const KEY = 'contentdock.theme'
const listeners = new Set<() => void>()

// Dark is the default. The saved choice is applied to <html> by a script in index.html before the first paint.
export function getTheme(): Theme {
  return document.documentElement.dataset.theme === 'light' ? 'light' : 'dark'
}

export function setTheme(theme: Theme) {
  document.documentElement.dataset.theme = theme

  try {
    localStorage.setItem(KEY, theme)
  } catch {
    // Storage blocked: the choice lasts until the page is closed.
  }

  listeners.forEach((listener) => listener())
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

export const useTheme = () => useSyncExternalStore(subscribe, getTheme)
