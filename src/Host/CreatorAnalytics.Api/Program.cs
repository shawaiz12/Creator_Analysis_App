using CreatorAnalytics.Api.BackgroundServices;
using CreatorAnalytics.Api.Middleware;
using CreatorAnalytics.Audit;
using CreatorAnalytics.Audit.Endpoints;
using CreatorAnalytics.Identity;
using CreatorAnalytics.Identity.Endpoints;
using CreatorAnalytics.SharedKernel.Tenancy;
using CreatorAnalytics.SharedKernel.Users;
using CreatorAnalytics.Strategy;
using CreatorAnalytics.Strategy.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using CreatorAnalytics.Integration;
using CreatorAnalytics.Integration.Endpoints;
using Microsoft.Extensions.AI;
using CreatorAnalytics.Api.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using CreatorAnalytics.SharedKernel.Data;
using CreatorAnalytics.Api.Ai;


var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}

// Per-request context: which tenant and which user this request belongs to.
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());


var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is missing.");

// Each module registers itself.
builder.Services.AddStrategyModule(connectionString);
builder.Services.AddIdentityModule(connectionString);
builder.Services.AddAuditModule(connectionString);
builder.Services.AddIntegrationModule(connectionString);
// Optional AI provider. With no Ai:Provider configured, the template writer stays in use.
builder.Services.AddConfiguredAi(builder.Configuration);


// JWT bearer authentication (Microsoft Entra ID).
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
    });
builder.Services.AddAuthorization();

builder.Services.AddOpenApi();

builder.Services.AddSingleton<OutboxDispatcher>();
builder.Services.AddHostedService<OutboxProcessorBackgroundService>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });

const string WebCorsPolicy = "Web";

builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

    options.AddPolicy(WebCorsPolicy, policy => policy
        .WithOrigins(origins)
        .WithHeaders("Authorization", "Content-Type")
        .WithMethods("GET", "POST", "PUT", "DELETE"));
});

var app = builder.Build();



// Optional: create or update the database schema on startup (used by the Docker setup).
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    foreach (var migrator in scope.ServiceProvider.GetServices<IDatabaseMigrator>())
    {
        logger.LogInformation("Applying database migrations for {Module}", migrator.Name);
        await migrator.MigrateAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(WebCorsPolicy);

// Identity and multi-tenancy pipeline. The order matters.
app.UseAuthentication();
app.UseMiddleware<TenantGatekeeperMiddleware>();
app.UseAuthorization();

app.MapStrategyEndpoints();
app.MapIdentityEndpoints();
app.MapAuditEndpoints();
app.MapIntegrationEndpoints();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();

public partial class Program { }