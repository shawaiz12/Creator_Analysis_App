# Content Dock (code name CreatorAnalytics): project status

Last updated: 2026-10-31

## Live

- App on Vercel, API on a Render free web service, PostgreSQL on Neon, AI on the free Gemini
  API. Decisions are in `docs/adr` (0007 to 0009 cover the move to free hosting).

## Done

- **Architecture:** .NET 10 modular monolith (Identity, Integration, Strategy, Audit), one
  PostgreSQL database with a schema per module; modules only use each other's Contracts.
- **Tenant isolation:** tenant in the URL, membership gatekeeper (404 for non-members), EF
  query filters that fail closed, isolation tests.
- **Strategy:** approval state machine, revisions frozen during review, reviews tied to the
  exact revision seen, concurrency protection, outbox event for every change.
- **Identity:** organizations, memberships, self-serve onboarding, invitations accepted only
  by the invited email, Microsoft Entra sign-in verified end to end.
- **Audit:** background dispatcher with retries and parking, Admin-only read endpoint.
- **Integration:** channels, videos, transcripts, retention snapshots, sync service (against a
  fake YouTube client).
- **AI drafts:** background worker, generate endpoint, Gemini through an OpenAI-compatible
  client with an optional fallback model, deterministic template writer when no AI is configured.
- **Frontend:** React, TypeScript, Vite, Tailwind, React Query and MSAL; overview, videos and
  retention, strategies with the approval workflow, team and invitations, audit trail.
- **Operations:** health endpoints, JSON logs, migrators, Docker, CI on PostgreSQL.

## Not done yet

1. Real Google OAuth and a real YouTube client (designed in ADR 0004).
2. Public sign-up for visitors; sign-in is limited to the owner's Microsoft tenant.
3. Invitation and onboarding actions are not in the audit trail.
4. Rate limiting, a per-organization AI draft cap and token accounting.
5. Visual redesign (Content Dock identity, dark theme, charts) and a demo mode.

## Run it

See `README.md`.