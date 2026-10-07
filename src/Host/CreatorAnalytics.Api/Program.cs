using CreatorAnalytics.Api.Middleware;
using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.Identity.Infrastructure;
using CreatorAnalytics.Identity.Services;
using CreatorAnalytics.SharedKernel.Tenancy;
using CreatorAnalytics.Strategy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using CreatorAnalytics.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

builder.Services.AddStrategyModule(
    builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is missing."));

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.Schema)));

// Register the Identity module's access service
builder.Services.AddScoped<ITenantAccessService, TenantAccessService>();

// Configure JWT Bearer Authentication (Microsoft Entra ID setup)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // These will be loaded from appsettings.json / user-secrets later
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
    });
builder.Services.AddAuthorization();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Identity and Multi-tenancy Pipeline
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantGatekeeperMiddleware>();

// Map module endpoints
app.MapStrategyEndpoints();

app.Run();