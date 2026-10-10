using System.Net;
using System.Net.Http.Json;

namespace CreatorAnalytics.Api.Tests;

public class OutboxPipelineApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public OutboxPipelineApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private Guid NewTenantWithAdmin(string adminExternalId)
    {
        var tenantId = Guid.NewGuid();
        _factory.Access.AddMember(tenantId, adminExternalId, Guid.NewGuid(), "Admin");
        return tenantId;
    }

    private async Task Approve(string adminExternalId, Guid tenantId, Guid documentId, Guid revisionId)
    {
        using var admin = _factory.ClientFor(adminExternalId);
        var response = await admin.PostAsJsonAsync(
            $"/api/{tenantId}/strategies/{documentId}/approve", new { revisionId });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Approving_Is_Audited_Only_After_The_Dispatcher_Runs()
    {
        var tenantId = NewTenantWithAdmin("admin-o1");
        var document = await _factory.SeedPendingDocumentAsync(tenantId);

        await Approve("admin-o1", tenantId, document.Id, document.CurrentRevision!.Id);

        // Eventual consistency: nothing has been audited yet.
        Assert.Empty(await _factory.GetAuditLogsAsync(tenantId));

        var handled = await _factory.RunOutboxAsync();

        Assert.True(handled >= 1);
        var audit = Assert.Single(await _factory.GetAuditLogsAsync(tenantId));
        Assert.Equal("StrategyApprovedEvent", audit.EventType);
        Assert.Equal(tenantId, audit.TenantId);
        Assert.Contains(document.Id.ToString(), audit.EventData);

        var message = Assert.Single(
            await _factory.GetOutboxMessagesAsync(),
            m => m.Content.Contains(document.Id.ToString()));
        Assert.NotNull(message.ProcessedOnUtc);
    }

    [Fact]
    public async Task A_Rejection_Is_Audited_With_Its_Reason()
    {
        var tenantId = NewTenantWithAdmin("admin-o2");
        var document = await _factory.SeedPendingDocumentAsync(tenantId);

        using var admin = _factory.ClientFor("admin-o2");
        var response = await admin.PostAsJsonAsync(
            $"/api/{tenantId}/strategies/{document.Id}/reject",
            new { revisionId = document.CurrentRevision!.Id, reason = "Hook is weak." });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await _factory.RunOutboxAsync();

        var audit = Assert.Single(await _factory.GetAuditLogsAsync(tenantId));
        Assert.Equal("StrategyRejectedEvent", audit.EventType);
        Assert.Contains("Hook is weak.", audit.EventData);
    }

    [Fact]
    public async Task Running_The_Dispatcher_Twice_Never_Duplicates_An_Audit_Row()
    {
        var tenantId = NewTenantWithAdmin("admin-o3");
        var document = await _factory.SeedPendingDocumentAsync(tenantId);
        await Approve("admin-o3", tenantId, document.Id, document.CurrentRevision!.Id);

        await _factory.RunOutboxAsync();
        await _factory.RunOutboxAsync();

        Assert.Single(await _factory.GetAuditLogsAsync(tenantId));
    }

    [Fact]
    public async Task A_Crash_After_Auditing_Is_Safely_Retried_Without_A_Duplicate()
    {
        var tenantId = NewTenantWithAdmin("admin-o4");
        var document = await _factory.SeedPendingDocumentAsync(tenantId);
        await Approve("admin-o4", tenantId, document.Id, document.CurrentRevision!.Id);

        // Simulate a crash: the audit row was saved, but the message was never marked as done.
        var message = (await _factory.GetOutboxMessagesAsync())
            .Single(m => m.Content.Contains(document.Id.ToString()));
        await _factory.AddAuditLogAsync(tenantId, message.Id, message.Type, message.Content);

        await _factory.RunOutboxAsync();

        Assert.Single(await _factory.GetAuditLogsAsync(tenantId));
        var after = (await _factory.GetOutboxMessagesAsync()).Single(m => m.Id == message.Id);
        Assert.NotNull(after.ProcessedOnUtc);
    }

    [Fact]
    public async Task Two_Tenants_In_One_Run_Are_Both_Audited_And_Kept_Apart()
    {
        var tenantA = NewTenantWithAdmin("admin-o5a");
        var tenantB = NewTenantWithAdmin("admin-o5b");
        var documentA = await _factory.SeedPendingDocumentAsync(tenantA);
        var documentB = await _factory.SeedPendingDocumentAsync(tenantB);
        await Approve("admin-o5a", tenantA, documentA.Id, documentA.CurrentRevision!.Id);
        await Approve("admin-o5b", tenantB, documentB.Id, documentB.CurrentRevision!.Id);

        await _factory.RunOutboxAsync();

        var auditA = Assert.Single(await _factory.GetAuditLogsAsync(tenantA));
        var auditB = Assert.Single(await _factory.GetAuditLogsAsync(tenantB));
        Assert.Contains(documentA.Id.ToString(), auditA.EventData);
        Assert.DoesNotContain(documentB.Id.ToString(), auditA.EventData);
        Assert.Contains(documentB.Id.ToString(), auditB.EventData);
    }

    [Fact]
    public async Task A_Malformed_Message_Does_Not_Block_The_Others()
    {
        var tenantId = NewTenantWithAdmin("admin-o6");
        await _factory.AddOutboxMessageAsync("Garbage", "{this is not json");
        var document = await _factory.SeedPendingDocumentAsync(tenantId);
        await Approve("admin-o6", tenantId, document.Id, document.CurrentRevision!.Id);

        await _factory.RunOutboxAsync();

        var audit = Assert.Single(await _factory.GetAuditLogsAsync(tenantId));
        Assert.Contains(document.Id.ToString(), audit.EventData);

        var garbage = (await _factory.GetOutboxMessagesAsync()).Single(m => m.Type == "Garbage");
        Assert.Null(garbage.ProcessedOnUtc);
    }
}