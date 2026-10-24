import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError } from './apiFetch'
import { tenantFetch, tenantKey } from './tenant'

export type Channel = {
  id: string
  youTubeChannelId: string
  title: string
  status: string
  lastSyncedAtUtc: string | null
}

export type Video = {
  id: string
  youTubeVideoId: string
  title: string
  publishedAtUtc: string
  durationSeconds: number
  hasTranscript: boolean
}

export type RetentionPoint = { elapsedRatio: number; audienceWatchRatio: number }

export type Retention = {
  videoId: string
  fetchedAtUtc: string
  points: RetentionPoint[]
}

export type SyncResult = { videosAdded: number; videosUpdated: number; snapshotsStored: number }

export function useChannels(tenantId: string) {
  return useQuery({
    queryKey: tenantKey(tenantId, 'channels'),
    queryFn: () => tenantFetch<Channel[]>(tenantId, '/channels'),
  })
}

export function useVideos(tenantId: string) {
  return useQuery({
    queryKey: tenantKey(tenantId, 'videos'),
    queryFn: () => tenantFetch<Video[]>(tenantId, '/videos'),
  })
}

// null means "no snapshot yet" (the API answers 404 until the first sync).
export function useRetention(tenantId: string, videoId: string) {
  return useQuery({
    queryKey: tenantKey(tenantId, 'retention', videoId),
    queryFn: async () => {
      try {
        return await tenantFetch<Retention>(tenantId, `/videos/${videoId}/retention`)
      } catch (error) {
        if (error instanceof ApiError && error.status === 404) return null
        throw error
      }
    },
  })
}

export function useConnectChannel(tenantId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: { youTubeChannelId: string; title: string }) =>
      tenantFetch<{ channelId: string }>(tenantId, '/channels', {
        method: 'POST',
        body: JSON.stringify(input),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tenantKey(tenantId, 'channels') }),
  })
}

export function useSyncChannel(tenantId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (channelId: string) =>
      tenantFetch<SyncResult>(tenantId, `/channels/${channelId}/sync`, { method: 'POST' }),
    // A sync can change channels, videos and retention, so refresh everything for this organization.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: tenantKey(tenantId) }),
  })
}