# 0010. Guest mode with a rate-limited live AI endpoint

Status: Accepted

## Context
The tool is shown to YouTube startups who should be able to try it. Sign-in is limited to the
owner's Microsoft tenant (ADR 0008), real public sign-up would need guardrails against abuse,
storage growth and AI quota, and the real YouTube connection is not built yet.

## Decision
- "Continue as guest" opens a demo organization (Northwind Studio). Its data lives in the browser:
  a small in-browser fake of the API sits behind the single data seam (`apiFetch`), with state in
  sessionStorage. Guests can switch between Admin, Strategist and Editor to see the approval rules.
  Nothing a guest does is stored on the server.
- Only AI drafts touch the server. An anonymous `POST /api/guest/insights` takes a video title,
  length, 100 retention points, an optional script (8,000 characters at most) and up to 5 rejection
  reasons, and returns a draft from the same generator the real app uses. It is stateless and has no
  database access.
- Limits: 5 drafts per visitor per day, 100 per day for everyone, 2 at a time, request size 48 KB.
  All are configurable under `Guest:*`.
- The visitor's address comes from the `CF-Connecting-IP` header. Render sits behind Cloudflare, the
  direct connection is a local proxy, and `X-Forwarded-For` is appended to rather than cleaned, so a
  visitor can forge it.

## Alternatives considered
- Real temporary accounts per guest: a second authentication system, cleanup jobs, growth of the
  free database, and an easier target for abuse.
- Open public sign-up: needs a multi-tenant identity setup and the same caps, which is future work.
- Pre-written drafts only: free and safe, but visitors could not test the AI.

## Consequences
- Counters are in memory, so they reset when the API restarts and need a single instance (like the
  background workers).
- A failed draft still counts against the limit.
- Many visitors on one mobile carrier can share an address. The per-visitor limit is a fairness
  layer; the daily limit for everyone is what protects the AI quota. A forged header can only
  affect fairness.
- Guests cannot analyze their own channel until the real YouTube connection exists.
- AI output is only ever shown as plain text. Drafts pre-loaded in the demo are labelled "sample draft".