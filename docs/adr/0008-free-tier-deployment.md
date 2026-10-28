# 0008. Free-tier deployment

Status: Accepted

## Context
The project is a public portfolio piece and must run at no cost indefinitely, without a
paid cloud subscription.

## Decision
- Frontend: Vercel (static Vite build, SPA fallback in `web/vercel.json`).
- API: Render free web service built from the repository Dockerfile, one instance.
- Database: Neon free PostgreSQL, direct (non-pooled) connection, Npgsql pool capped at 20
  so it stays under the free connection limit (see ADR 0007).
- Configuration comes from environment variables set in each provider's dashboard. No
  secret is stored in the repository. `VITE_` values are public by design.
- Migrations run on start-up (`Database__MigrateOnStartup`), safe because there is exactly one
  instance.
- Entra ID: the Vercel address is registered as a Single-page application redirect URI;
  the API's CORS allow-list contains the Vercel address only.

## Consequences
- The API sleeps after 15 minutes without traffic and takes about a minute to wake. The
  outbox, generation and sync workers only run while it is awake; queued work waits for the
  next request. The transactional outbox makes this safe, only slower.
- Free services can be restarted at any time, so nothing may depend on local files.
- Neon free keeps 6 hours of history, which is not a backup strategy.
- Moving to always-on hosting later is a configuration change, not a code change.