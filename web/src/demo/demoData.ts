import type { Channel, RetentionPoint, Video } from '../api/integration'

const minutesAgo = (minutes: number) => new Date(Date.now() - minutes * 60_000).toISOString()
const daysAgo = (days: number) => minutesAgo(days * 24 * 60)

// ---- Sample data: a fictional channel called Northwind Studio ----

export const demoChannel: Channel = {
  id: '7f3b0000-0000-4000-8000-0000000000c1',
  youTubeChannelId: 'UC-northwind-demo',
  title: 'Northwind Studio',
  status: 'Active',
  lastSyncedAtUtc: minutesAgo(95),
}

export const DEMO_VIDEO_IDS = {
  setup: '7f3b0002-0000-4000-8000-000000000002',
  intros: '7f3b0003-0000-4000-8000-000000000003',
}

type Drop = { at: number; size: number }

type SampleVideo = Omit<Video, 'publishedAtUtc'> & {
  daysAgo: number
  floor: number
  drops: Drop[]
  transcript: string | null
}

const sampleVideos: SampleVideo[] = [
  {
    id: '7f3b0001-0000-4000-8000-000000000001',
    youTubeVideoId: 'nw-demo-001',
    title: 'How I Edit a 10-Minute Video in 90 Minutes',
    daysAgo: 4,
    durationSeconds: 742,
    hasTranscript: true,
    floor: 0.3,
    drops: [{ at: 0.06, size: 0.09 }, { at: 0.47, size: 0.07 }],
    transcript:
      "Hey everyone, welcome back to the channel. Before we start, a quick word about today's sponsor, the three videos I mentioned last week, and a reminder to subscribe and hit the bell. Okay, so today I'm showing you my full editing workflow. First I import and sort every clip into bins by scene. Second, I make a rough cut using only the best takes. Then I spend almost two minutes explaining my folder naming system in detail, which is a bit boring but saves me hours. After that comes the fun part: pacing, music, sound design, and finally the export settings I use for YouTube.",
  },
  {
    id: DEMO_VIDEO_IDS.setup,
    youTubeVideoId: 'nw-demo-002',
    title: 'The $300 Studio Setup (Everything I Use)',
    daysAgo: 11,
    durationSeconds: 596,
    hasTranscript: true,
    floor: 0.34,
    drops: [{ at: 0.12, size: 0.08 }, { at: 0.71, size: 0.06 }],
    transcript:
      "What's up, today I'm showing you my entire studio setup for under three hundred dollars. But first, let me tell you how I got into video. It goes back to 2016, when I borrowed my cousin's camera and made a terrible video about my hometown, and then another one, and honestly the first year was rough. Anyway, here is the desk, the light, the microphone and the backdrop. Near the end I read out every affiliate link one by one, so you can find each item, and then I show the final shot of the finished room.",
  },
  {
    id: DEMO_VIDEO_IDS.intros,
    youTubeVideoId: 'nw-demo-003',
    title: 'Why Your Intros Lose 40% of Viewers',
    daysAgo: 19,
    durationSeconds: 488,
    hasTranscript: true,
    floor: 0.36,
    drops: [{ at: 0.04, size: 0.1 }, { at: 0.33, size: 0.05 }],
    transcript:
      "Most intros are way too long, and in this video I'll explain why. First, let me tell you who I am and what this channel is about, and then a quick history of the YouTube intro, because it helps to understand where all this came from. Right, the real problem is the first twenty seconds. Viewers decide very fast whether the video is for them. In the middle I compare three intro styles from other channels, and at the end I rebuild my own intro using what we learned.",
  },
  {
    id: '7f3b0004-0000-4000-8000-000000000004',
    youTubeVideoId: 'nw-demo-004',
    title: 'Thumbnail Mistakes I Made for 3 Years',
    daysAgo: 33,
    durationSeconds: 531,
    hasTranscript: false,
    floor: 0.28,
    drops: [{ at: 0.18, size: 0.07 }],
    transcript: null,
  },
  {
    id: '7f3b0005-0000-4000-8000-000000000005',
    youTubeVideoId: 'nw-demo-005',
    title: 'Cheap Mics vs Expensive Mics: Blind Test',
    daysAgo: 47,
    durationSeconds: 874,
    hasTranscript: true,
    floor: 0.32,
    drops: [{ at: 0.09, size: 0.08 }, { at: 0.55, size: 0.09 }],
    transcript:
      "Today we test five microphones in a blind test. I'll start by reading out the full specification of each one: frequency response, polar pattern, sensitivity, and the connector type. Then we record the same paragraph on every mic. I won't tell you which is which until the end. Halfway through there is a long pause while I explain how I set up the levels, and only then do I reveal which mic cost the most and which one most people picked as the best.",
  },
  {
    id: '7f3b0006-0000-4000-8000-000000000006',
    youTubeVideoId: 'nw-demo-006',
    title: 'Monthly Channel Report: What Actually Grew',
    daysAgo: 62,
    durationSeconds: 948,
    hasTranscript: true,
    floor: 0.26,
    drops: [{ at: 0.14, size: 0.09 }, { at: 0.62, size: 0.07 }],
    transcript:
      "Welcome to the monthly report. This month we grew by about four thousand subscribers. Let me start with the overall numbers: views, watch time, impressions, and click-through rate for the whole month. Then the best performing video and why I think it worked. Around the middle I go through the revenue breakdown line by line, and finally I share what I plan to change next month and the three experiments I want to run.",
  },
]

function buildCurve(drops: Drop[], floor: number): RetentionPoint[] {
  const points: RetentionPoint[] = []

  for (let i = 0; i < 100; i++) {
    const elapsedRatio = (i + 1) / 100
    let watch = floor + (1 - floor) * Math.exp(-2.4 * elapsedRatio)

    for (const drop of drops) {
      // Each drop is a smooth step over about 3% of the video.
      const progress = Math.min(1, Math.max(0, (elapsedRatio - drop.at) / 0.03))
      watch -= drop.size * progress * progress * (3 - 2 * progress)
    }

    // A little wobble so the curve does not look machine-made.
    watch += Math.sin(i * 1.7) * 0.004

    points.push({ elapsedRatio, audienceWatchRatio: Math.min(1, Math.max(0.02, watch)) })
  }

  return points
}

export const demoVideos: Video[] = sampleVideos.map((v) => ({
  id: v.id,
  youTubeVideoId: v.youTubeVideoId,
  title: v.title,
  publishedAtUtc: daysAgo(v.daysAgo),
  durationSeconds: v.durationSeconds,
  hasTranscript: v.hasTranscript,
}))

export const demoCurves = new Map<string, RetentionPoint[]>(sampleVideos.map((v) => [v.id, buildCurve(v.drops, v.floor)]))

export const demoTranscripts = new Map<string, string>(
  sampleVideos.flatMap((v): [string, string][] => (v.transcript ? [[v.id, v.transcript]] : [])),
)
