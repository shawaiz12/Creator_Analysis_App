using CreatorAnalytics.Api.BackgroundServices;
using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.SharedKernel.Tenancy;
using CreatorAnalytics.Strategy.Domain;
using CreatorAnalytics.Strategy.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CreatorAnalytics.Identity.Infrastructure;
using CreatorAnalytics.Identity.Domain;
using CreatorAnalytics.Audit.Domain;
using CreatorAnalytics.Audit.Infrastructure;

namespace CreatorAnalytics.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _testConnection;
    public FakeTenantAccessService Access { get; } = new();

    public ApiFactory()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets("6a8817ab-906e-4a7b-acd9-f3e6c3b62f50")
            .Build();

        var baseConnection = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found in user secrets.");

        var testConnection = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"CreatorAnalytics_ApiTest_{Guid.NewGuid():N}"
        }.ConnectionString;

        _testConnection = testConnection;

        // Environment variables beat user-secrets, so the app under test
        // talks to a temporary database instead of your real one.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", testConnection);

        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<StrategyDbContext>().Database.Migrate();
        scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.Migrate();
        scope.ServiceProvider.GetRequiredService<AuditDbContext>().Database.Migrate();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // No background outbox processing during these tests.
            var hosted = services.SingleOrDefault(
                d => d.ImplementationType == typeof(OutboxProcessorBackgroundService));
            if (hosted is not null)
                services.Remove(hosted);

            // Fake sign-in instead of Microsoft Entra ID.
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            // Fake membership lookup.
            services.RemoveAll<ITenantAccessService>();
            services.AddSingleton<ITenantAccessService>(Access);
        });
    }

    public HttpClient ClientFor(string? externalUserId)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        if (externalUserId is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, externalUserId);

        return client;
    }

    public async Task<StrategyDocument> SeedPendingDocumentAsync(Guid tenantId)
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TenantContext>().Set(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<StrategyDbContext>();

        var document = new StrategyDocument(tenantId, Guid.NewGuid());
        document.AddRevision("AI draft", RevisionOrigin.Ai, null);
        document.SubmitForApproval(Guid.NewGuid());

        document.ClearDomainEvents();

        context.Documents.Add(document);
        await context.SaveChangesAsync();
        return document;
    }

    public async Task<StrategyDocument> GetDocumentAsync(Guid tenantId, Guid id)
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TenantContext>().Set(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<StrategyDbContext>();

        return await context.Documents
            .Include(d => d.Revisions)
            .Include(d => d.Reviews)
            .SingleAsync(d => d.Id == id);
    }

    public async Task<(Guid TenantId, Guid UserId)> SeedOrganizationWithAdminAsync(string adminExternalId)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var organization = new Organization($"Org {Guid.NewGuid():N}");
        var admin = new User(adminExternalId, $"{adminExternalId}@example.com");

        context.Organizations.Add(organization);
        context.Users.Add(admin);
        context.TenantMemberships.Add(new TenantMembership(organization.Id, admin.Id, Role.Admin));
        await context.SaveChangesAsync();

        Access.AddMember(organization.Id, adminExternalId, admin.Id, "Admin");
        return (organization.Id, admin.Id);
    }

    private bool _databaseDropped;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && !_databaseDropped)
        {
            _databaseDropped = true;
            DropTestDatabase();
        }
    }

    private void DropTestDatabase()
    {
        var builder = new SqlConnectionStringBuilder(_testConnection);
        var database = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        using var connection = new SqlConnection(builder.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            $"IF DB_ID(N'{database}') IS NOT NULL " +
            $"BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
            $"DROP DATABASE [{database}]; END";
        command.ExecuteNonQuery();
    }

    public Task<int> RunOutboxAsync() =>
    Services.GetRequiredService<OutboxDispatcher>().ProcessPendingAsync();

    public async Task<List<AuditLog>> GetAuditLogsAsync(Guid tenantId)
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TenantContext>().Set(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        return await context.AuditLogs.AsNoTracking().ToListAsync();
    }

    public async Task<List<OutboxMessage>> GetOutboxMessagesAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StrategyDbContext>();

        return await context.OutboxMessages.AsNoTracking().ToListAsync();
    }

    public async Task AddOutboxMessageAsync(string type, string content)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StrategyDbContext>();

        context.OutboxMessages.Add(new OutboxMessage(type, content));
        await context.SaveChangesAsync();
    }

    public async Task AddAuditLogAsync(Guid tenantId, Guid messageId, string type, string data)
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TenantContext>().Set(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        context.AuditLogs.Add(new AuditLog(tenantId, messageId, type, data));
        await context.SaveChangesAsync();
    }
}