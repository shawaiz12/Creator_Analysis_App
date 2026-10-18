# CreatorAnalytics: project status

Last updated: <put today's date here>

## Done (all covered by automated tests)

- **Architecture:** .NET 10 modular monolith with four modules (Identity, Integration,
  Strategy, Audit), one SQL Server database with a schema per module. Decisions are in
  `docs/adr`.
- **Tenant isolation:** the tenant is in the URL; a gatekeeper checks membership on every
  request (404 for non-members); EF query filters fail closed; cross-tenant writes throw a
  dedicated exception; isolation tests prove it.
- **Strategy module:** approval state machine, revisions that freeze during review, reviews
  tied to the exact revision seen, concurrency protection, and an outbox event for every
  state change. Endpoints live in the module.
- **Identity module:** users, organizations, memberships, self-serve onboarding, and
  invitations accepted only by the invited email address.
- **Audit module:** background dispatcher (audit first, then mark done; safe to retry;
  parks a message after 5 failed attempts) and an Admin-only read endpoint.
- **Rules enforced by the build:** modules only use each other's Contracts; the Api host
  never touches module internals; no `IgnoreQueryFilters` or raw SQL in the source.
  **Identity:** \GET /api/me` lists the signed-in user’s organizations and roles.

  -- **Integration module:** channel connections, videos, transcripts and 100-point retention
  snapshots with tenant isolation; a sync service (insert or update by YouTube video id,
  retention history, reconnect handling); endpoints for Admins and Strategists; a contract
  that lets Strategy check a video exists in the same organization. Runs against a fake
  YouTube client for now.

  - **AI insights (foundations):** generation requests inside the strategy (one open request at
  a time, at most 5 AI drafts, retry backoff, database-enforced), a retention analyzer that
  finds the biggest drop-offs with timestamps, and a deterministic template generator behind
  `IInsightGenerator`. The background worker, the generate endpoint and the real AI
  generator come next.

## Not done yet



1. ~~Real Microsoft Entra sign-in~~ verified by hand end to end (two tenant users, device
   login, organization created, invitation accepted, strategy approved, audit trail read).
   Automated tests still use a fake sign-in.
2. 2. Integration module: endpoints, scheduled sync, real Google OAuth and client.
3. AI insights: `IInsightGenerator`, background worker, rejection reasons as context.
4. Identity actions (invitations, joining) are not in the audit trail yet.
5. No CI pipeline, health checks, rate limiting, deployment, or user interface.
6. Planned last: extract the AI part into a stateless FastAPI service.


## Run it

See `README.md`.