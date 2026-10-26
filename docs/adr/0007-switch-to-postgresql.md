# 0007. Switch the database from SQL Server to PostgreSQL

Status: Accepted

## Context
The application ran on SQL Server. The goal is now a public, always-available portfolio
deployment at no cost. Free hosting for SQL Server does not suit this workload: the free
Azure SQL offer pauses when idle, which conflicts with the background workers that poll the
database (outbox, draft generation, channel sync). PostgreSQL has free hosted tiers (Neon),
and the code already isolated the provider choice in each module's registration.

## Decision
- Every module now uses PostgreSQL through Npgsql. The provider is chosen in one place per
  module (`AddXModule`), so nothing outside the infrastructure layer changed. ADR 0001
  (one database, one schema per module) still holds.
- The optimistic-concurrency token on `StrategyDocument` moved from SQL Server's `rowversion`
  to PostgreSQL's built-in `xmin` system column, mapped as a `uint` concurrency token.
  Behaviour is unchanged: a stale write raises `DbUpdateConcurrencyException`.
- Filtered indexes use PostgreSQL quoting (`"Status" = 'Pending'`).
- Migrations were regenerated from scratch as one `InitialCreate` per module. This was safe
  because no production database existed yet. Existing history was not translated.
- The health check, docker-compose and CI service container all use PostgreSQL 15.
- Test databases get short lowercase names (PostgreSQL limits names to 63 bytes) and are
  dropped with `DROP DATABASE ... WITH (FORCE)` after pooled connections are cleared.
- Tests still run against a real database engine, not an in-memory substitute, so concurrency,
  constraints and filtered indexes are exercised for real.

## Consequences
- The whole test suite passed unchanged apart from the database set-up and one timestamp
  comparison that needed a one-millisecond tolerance (PostgreSQL stores microseconds, .NET
  ticks are finer).
- Text comparisons are case-sensitive in PostgreSQL by default. No test relied on the old
  case-insensitive behaviour, but new code should not assume it.
- Old SQL Server migration history is gone. Anyone with an existing SQL Server database must
  recreate it.

## Open questions
- Whether to use a pooled connection string (Neon's pooler) in deployment. Decided in the
  deployment phase.