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

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Identity and multi-tenancy pipeline. The order matters.
app.UseAuthentication();
app.UseMiddleware<TenantGatekeeperMiddleware>();
app.UseAuthorization();

app.MapStrategyEndpoints();
app.MapIdentityEndpoints();
app.MapAuditEndpoints();

app.Run();

public partial class Program { }