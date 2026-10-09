# 0002. Tenant isolation

Status: Accepted

## Context
Many organizations share one system, and one organization must never see
another's data, even if a developer makes a mistake.

## Decision
- The tenant is part of the URL: /api/{tenantId}/... This avoids two browser
  tabs overwriting each other's tenant and supports deep links.
- A gatekeeper checks membership in the database on every request. Non-members
  and malformed tenant ids get 404, so the API never reveals which
  organizations exist. The sign-in token proves identity only; roles are never
  read from the token.
- The tenant for a request can be set once and never changed.
- EF Core query filters add the tenant condition to every read, and fail closed
  (no tenant means no data). Saving a record that belongs to another tenant is
  blocked.
- A guard test fails the build if IgnoreQueryFilters or raw SQL appears in the
  source. Isolation tests prove one tenant cannot read or write another's data.

## Consequences
- Isolation holds even when a query forgets to filter.
- It is not absolute: database access outside the guarded paths could bypass
  it, which is why the guard test exists and Row-Level Security is a future
  option.
- Background work has no web request, so it must set its tenant explicitly.