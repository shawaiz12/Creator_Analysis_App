# 0001. Modular monolith with one relational database

Status: Accepted

## Context
CreatorAnalytics needs strict data consistency (approvals must always leave an
audit record) and is built by one developer who is still learning backend design.

## Decision
One deployable ASP.NET Core application split into four modules: Identity,
Integration, Strategy, Audit. Each module has an implementation project and a
small Contracts project. Modules reference each other only through Contracts and
IDs. All modules share one SQL Server database, each in its own schema.
Architecture tests fail the build on forbidden references.

The AI capability sits behind an interface (IInsightGenerator). Extracting it
into a stateless Python FastAPI service is a planned later phase.

## Alternatives considered
- Microservices from day one: rejected, too much operational cost before any
  real need to scale parts independently.
- Cosmos DB for strategy data: rejected, a status change in one store and an
  audit record in another cannot share a transaction, so a crash could leave an
  approval without a record.

## Consequences
- Business changes and their audit records commit atomically.
- Module boundaries depend on discipline, so they are enforced by tests.
- Modules cannot scale independently until a module is extracted.