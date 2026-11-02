import type { RetentionPoint } from '../api/integration'

export type Drop = { startRatio: number; endRatio: number; pointsLost: number }

// The same method the API uses for the AI prompt (RetentionAnalyzer): ten equal windows,
// biggest losses first, losses under two percentage points ignored.
export function retentionDrops(points: RetentionPoint[], top = 2, minimumPointsLost = 2): Drop[] {
  if (points.length !== 100) return []

  const windows: Drop[] = []

  for (let start = 0; start < 100; start += 10) {
    const end = start + 9
    const before = start === 0 ? 1 : points[start - 1].audienceWatchRatio
    const lost = (before - points[end].audienceWatchRatio) * 100

    if (lost < minimumPointsLost) continue

    windows.push({
      startRatio: start === 0 ? 0 : points[start - 1].elapsedRatio,
      endRatio: points[end].elapsedRatio,
      pointsLost: Math.round(lost * 10) / 10,
    })
  }

  return windows.sort((a, b) => b.pointsLost - a.pointsLost || a.startRatio - b.startRatio).slice(0, top)
}

export function clock(totalSeconds: number) {
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = String(Math.round(totalSeconds % 60)).padStart(2, '0')
  return `${minutes}:${seconds}`
}
