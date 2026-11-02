import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useChannels, useConnectChannel, useSyncChannel, useVideos } from '../api/integration'
import { canConnectChannel } from './roles'
import { useTenant } from './useTenant'

function formatDuration(totalSeconds: number) {
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = String(totalSeconds % 60).padStart(2, '0')
  return `${minutes}:${seconds}`
}

function ConnectChannelForm() {
  const { tenantId } = useTenant()
  const connect = useConnectChannel(tenantId)
  const [youTubeChannelId, setYouTubeChannelId] = useState('')
  const [title, setTitle] = useState('')

  return (
    <form
      className="mt-3 rounded border border-dashed bg-surface p-3"
      onSubmit={(event) => {
        event.preventDefault()
        connect.mutate(
          { youTubeChannelId: youTubeChannelId.trim(), title: title.trim() },
          {
            onSuccess: () => {
              setYouTubeChannelId('')
              setTitle('')
            },
          },
        )
      }}
    >
      <p className="text-sm font-medium">Register a channel (development shortcut)</p>
      <p className="text-xs text-subtle">The real flow will connect through Google sign-in.</p>
      <div className="mt-2 flex flex-wrap gap-2">
        <input
          className="min-w-40 flex-1 rounded border px-3 py-2 text-sm"
          placeholder="YouTube channel id"
          value={youTubeChannelId}
          onChange={(event) => setYouTubeChannelId(event.target.value)}
        />
        <input
          className="min-w-40 flex-1 rounded border px-3 py-2 text-sm"
          placeholder="Channel title"
          value={title}
          onChange={(event) => setTitle(event.target.value)}
        />
        <button
          type="submit"
          disabled={connect.isPending || !youTubeChannelId.trim() || !title.trim()}
          className="rounded bg-primary px-4 py-2 text-sm font-medium text-on-solid hover:bg-primary-hover disabled:opacity-50"
        >
          {connect.isPending ? 'Saving…' : 'Register'}
        </button>
      </div>
      {connect.isError && <p className="mt-2 text-sm text-bad">{connect.error.message}</p>}
    </form>
  )
}

function Channels() {
  const { tenantId, role } = useTenant()
  const channels = useChannels(tenantId)
  const sync = useSyncChannel(tenantId)

  return (
    <section>
      <h3 className="text-lg font-semibold">Channels</h3>

      {channels.isPending && <p className="mt-2 text-subtle">Loading channels…</p>}
      {channels.isError && <p className="mt-2 text-bad">{channels.error.message}</p>}

      {channels.data && channels.data.length === 0 && (
        <p className="mt-2 text-muted">No channel is connected yet.</p>
      )}

      {channels.data && channels.data.length > 0 && (
        <ul className="mt-3 space-y-2">
          {channels.data.map((channel) => (
            <li key={channel.id} className="flex items-center justify-between gap-3 rounded border bg-surface p-3">
              <div>
                <p className="font-medium">{channel.title}</p>
                <p className="text-xs text-subtle">
                  {channel.status}
                  {' · '}
                  {channel.lastSyncedAtUtc
                    ? `last synced ${new Date(channel.lastSyncedAtUtc).toLocaleString()}`
                    : 'never synced'}
                </p>
              </div>
              <button
                className="rounded border px-3 py-1 text-sm hover:bg-raised disabled:opacity-50"
                disabled={sync.isPending}
                onClick={() => sync.mutate(channel.id)}
              >
                {sync.isPending && sync.variables === channel.id ? 'Syncing…' : 'Sync now'}
              </button>
            </li>
          ))}
        </ul>
      )}

      {sync.isSuccess && (
        <p className="mt-2 text-sm text-ok">
          Sync finished: {sync.data.videosAdded} added, {sync.data.videosUpdated} updated,{' '}
          {sync.data.snapshotsStored} retention snapshots stored.
        </p>
      )}
      {sync.isError && <p className="mt-2 text-sm text-bad">{sync.error.message}</p>}

      {canConnectChannel(role) && <ConnectChannelForm />}
    </section>
  )
}

function VideoList() {
  const { tenantId } = useTenant()
  const videos = useVideos(tenantId)

  return (
    <section className="mt-8">
      <h3 className="text-lg font-semibold">Videos</h3>

      {videos.isPending && <p className="mt-2 text-subtle">Loading videos…</p>}
      {videos.isError && <p className="mt-2 text-bad">{videos.error.message}</p>}

      {videos.data && videos.data.length === 0 && (
        <p className="mt-2 text-muted">No videos yet. Sync a channel to import them.</p>
      )}

      {videos.data && videos.data.length > 0 && (
        <ul className="mt-3 space-y-2">
          {videos.data.map((video) => (
            <li key={video.id}>
              <Link
                to={`/orgs/${tenantId}/videos/${video.id}`}
                className="flex items-center justify-between gap-3 rounded border bg-surface p-3 hover:border-line-strong"
              >
                <div>
                  <p className="font-medium">{video.title}</p>
                  <p className="text-xs text-subtle">
                    {new Date(video.publishedAtUtc).toLocaleDateString()} · {formatDuration(video.durationSeconds)}
                  </p>
                </div>
                <span
                  className={`rounded px-2 py-0.5 text-xs ${
                    video.hasTranscript ? 'bg-ok-soft text-ok' : 'bg-raised text-muted'
                  }`}
                >
                  {video.hasTranscript ? 'Transcript' : 'No transcript'}
                </span>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

export function Videos() {
  return (
    <div>
      <Channels />
      <VideoList />
    </div>
  )
}