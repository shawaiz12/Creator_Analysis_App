# 0004. YouTube integration

Status: Proposed

## Context
CreatorAnalytics needs each channel's videos, per-video audience retention, and a
transcript to feed the AI. The data comes from Google, which means OAuth consent from the
channel owner and a stored refresh token.

Facts checked when this was written:
- The YouTube retention report returns 100 points per video (elapsed fraction of the video
  from 0.01 to 1.0, with the share of viewers still watching).
- A Google OAuth app in "Testing" status allows at most 100 test users, and authorizations
  (including refresh tokens) expire seven days after consent. Production use of sensitive
  scopes needs Google's verification.

## Decision (proposed)
- The Integration module talks to YouTube only through `IYouTubeClient` (list videos, get
  retention). A fake implementation comes first; the Google implementation is added later.
- OAuth uses the authorization code flow: one endpoint returns Google's sign-in URL with a
  signed `state` (tenant, user, expiry), another handles the callback and validates it.
- The refresh token is encrypted at rest, never logged, never returned by the API.
- A channel connection is `Connected` or `NeedsReconnect`. A rejected refresh token moves
  it to `NeedsReconnect`; this is expected weekly while the app is in Testing.
- Retention is stored as one JSON array of 100 points per video per fetch.
- The transcript is supplied by the user at first; automatic caption download is deferred.
- Ingestion is a scheduled background job per connected channel with an explicit tenant
  scope, writing idempotently (insert or update by YouTube video id).

## Consequences
- The module can be built and tested before any Google credentials exist.
- Users must reconnect about weekly until the app passes Google verification.
- The encryption key must be persisted properly when deployed.

## Open questions
- Where the transcript is stored (probably with the video).
- Which secret store holds the encryption key in production.