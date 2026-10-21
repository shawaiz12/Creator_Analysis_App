# CreatorAnalytics

Multi-tenant SaaS for YouTube channel teams: ingests video performance data and
uses AI to propose strategy fixes. Every AI strategy needs human approval
(human-in-the-loop) before it is final.

Architecture: .NET 10 modular monolith (Identity, Integration, Strategy, Audit),
one SQL Server database with a schema per module.

[![CI](https://github.com/shawaiz12/Creator_Analysis_App/actions/workflows/ci.yml/badge.svg)](https://github.com/shawaiz12/Creator_Analysis_App/actions/workflows/ci.yml)

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

  ## Try it with real Microsoft sign-in

1. In the Microsoft Entra admin center, register an app named `CreatorAnalytics API`
   (single tenant). Under **Expose an API**, set the Application ID URI and add a scope
   called `access_as_user`. In the **Manifest**, set the access token version to `2`.
   Under **Authentication**, allow public client flows. Do not create a client secret:
   the API only validates tokens.
2. Create a couple of test users in your tenant.
3. Store the trust settings (they are not secrets, but they are specific to your tenant):

```powershell
   dotnet user-secrets set "Authentication:Authority" "https://login.microsoftonline.com/<tenant-id>/v2.0" --project src\Host\CreatorAnalytics.Api
   dotnet user-secrets set "Authentication:Audience" "<application-client-id>" --project src\Host\CreatorAnalytics.Api
```

4. Start the API: `dotnet run --project src\Host\CreatorAnalytics.Api --launch-profile https`
5. Get a token with the device login flow (sign in with the browser when prompted):

```powershell
   $tenant = "<tenant-id>"; $client = "<application-client-id>"
   $scope  = "api://$client/access_as_user"
   $device = Invoke-RestMethod -Method Post -Uri "https://login.microsoftonline.com/$tenant/oauth2/v2.0/devicecode" -Body @{ client_id = $client; scope = $scope }
   $device.message
   # after signing in:
   $token = Invoke-RestMethod -Method Post -Uri "https://login.microsoftonline.com/$tenant/oauth2/v2.0/token" -Body @{ grant_type = "urn:ietf:params:oauth:grant-type:device_code"; client_id = $client; device_code = $device.device_code }
   $headers = @{ Authorization = "Bearer $($token.access_token)" }
```

6. Call the API, for example:

```powershell
   $org = Invoke-RestMethod -Method Post -Uri "https://localhost:<port>/api/onboarding/organizations" -Headers $headers -ContentType "application/json" -Body '{"name":"Acme Studio"}'
   Invoke-RestMethod -Method Get -Uri "https://localhost:<port>/api/me" -Headers $headers
```

Never paste a token anywhere: it is a live credential for about an hour. In Windows
PowerShell 5.1, a JSON array piped straight into `Select-Object` can display blank
columns; store the result in a variable first.

## Optional: write drafts with a local AI model

Install Ollama (ollama.com), then `ollama pull phi3:mini`. Tell the API to use it:

```powershell
dotnet user-secrets set "Ai:Provider" "Ollama" --project src\Host\CreatorAnalytics.Api
dotnet user-secrets set "Ai:Model" "phi3:mini" --project src\Host\CreatorAnalytics.Api
```

Without these settings the deterministic template writer is used. Nothing leaves your
machine: the model sees only the video title, the transcript, the computed drop-off facts and
earlier rejection reasons.