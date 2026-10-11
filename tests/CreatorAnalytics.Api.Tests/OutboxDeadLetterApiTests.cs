using System.Net;
using System.Net.Http.Json;
using CreatorAnalytics.Strategy.Domain;

namespace CreatorAnalytics.Api.Tests;

public class OutboxDeadLetterApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public OutboxDeadLetterApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task A_Message_That_Keeps_Failing_Is_Parked_After_The_Attempt_Cap()
    {
        await _factory.AddOutboxMessageAsync("Garbage-single", "{this is not json");

        for (var i = 0; i < OutboxMessage.MaxAttempts + 2; i++)
            await _factory.RunOutboxAsync();

        var parked = (await _factory.GetOutboxMessagesAsync()).Single(m => m.Type == "Garbage-single");
        Assert.Equal(OutboxMessage.MaxAttempts, parked.Attempts);
        Assert.NotNull(parked.Error);
        Assert.Null(parked.ProcessedOnUtc);
    }

    [Fact]
    public async Task Parked_Messages_Do_Not_Starve_Healthy_Ones()
    {
        // More broken messages than fit in one batch, all older than the healthy one.
        for (var i = 0; i < 25; i++)
            await _factory.AddOutboxMessageAsync($"Garbage-flood-{i}", "{this is not json");

        var tenantId = Guid.NewGuid();
        _factory.Access.AddMember(tenantId, "admin-d2", Guid.NewGuid(), "Admin");
        var document = await _factory.SeedPendingDocumentAsync(tenantId);

        using var admin = _factory.ClientFor("admin-d2");
        var approved = await admin.PostAsJsonAsync(
            $"/api/{tenantId}/strategies/{document.Id}/approve",
            new { revisionId = document.CurrentRevision!.Id });
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);

        // Enough runs for every broken message to use up its attempts and get parked.
        for (var i = 0; i < 14; i++)
            await _factory.RunOutboxAsync();

        var audit = Assert.Single(await _factory.GetAuditLogsAsync(tenantId));
        Assert.Equal("StrategyApprovedEvent", audit.EventType);
    }
}