# CreatorAnalytics

Multi-tenant SaaS for YouTube channel teams: ingests video performance data and
uses AI to propose strategy fixes. Every AI strategy needs human approval
(human-in-the-loop) before it is final.

Architecture: .NET 10 modular monolith (Identity, Integration, Strategy, Audit),
one SQL Server database with a schema per module.

## Run it locally

1. Create a `.env` file in the repo root (it is git-ignored). Copy `.env.example`
   and set `SQL_SA_PASSWORD` to a strong password. Avoid `; = $ " '` and spaces.
2. Start the database: `docker compose up -d`
3. Store the connection string in user-secrets (never in Git). Use the same password:
   `dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,14333;Database=CreatorAnalytics;User Id=sa;Password=<password>;TrustServerCertificate=True" --project src\Host\CreatorAnalytics.Api`
4. Apply the migrations:
   `$env:ASPNETCORE_ENVIRONMENT = "Development"`
   `dotnet ef database update --project src\Modules\Strategy\CreatorAnalytics.Strategy --startup-project src\Host\CreatorAnalytics.Api --context StrategyDbContext`
5. Run the tests: `dotnet test`

## Troubleshooting

- Login failed (error 18456): the container keeps the first `sa` password in its
  volume. If it no longer matches `.env`, run `docker compose down -v` (deletes
  local data) and `docker compose up -d`.
- Timeout (error 258): the container is not running or not ready yet. Check
  `docker logs creatoranalytics-sql` for "ready for client connections".