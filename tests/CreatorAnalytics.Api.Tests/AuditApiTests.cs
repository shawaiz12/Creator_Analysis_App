using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CreatorAnalytics.Api.Endpoints;

namespace CreatorAnalytics.Api.Tests;

public sealed record AuditEntryResponse(Guid Id, string EventType, DateTime RecordedAtUtc, JsonElement Data);

public class AuditApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuditApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task An_Admin_Can_Read_The_Whole_Story_Of_A_Strategy()
    {
        var tenantId = Guid.NewGuid();
        var strategistUser = Guid.NewGuid();
        var editorUser = Guid.NewGuid();
        _factory.Access.AddMember(tenantId, "admin-u1", Guid.NewGuid(), "Admin");
        _factory.Access.AddMember(tenantId, "strategist-u1", strategistUser, "Strategist");
        _factory.Access.AddMember(tenantId, "editor-u1", editorUser, "Editor");

        using var admin = _factory.ClientFor("admin-u1");
        using var strategist = _factory.ClientFor("strategist-u1");
        using var editor = _factory.ClientFor("editor-u1");
        var baseUrl = $"/api/{tenantId}/strategies";

        var created = await strategist.PostAsync(baseUrl, null);
        var id = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.DocumentId;

        var revisionResponse = await strategist.PostAsJsonAsync(
            $"{baseUrl}/{id}/revisions", new { content = "Open with the result." });
        var revision = (await revisionResponse.Content.ReadFromJsonAsync<RevisionDto>())!;

        await strategist.PostAsync($"{baseUrl}/{id}/submit", null);
        await admin.PostAsJsonAsync($"{baseUrl}/{id}/approve", new { revisionId = revision.Id });
        await editor.PostAsync($"{baseUrl}/{id}/implemented", null);

        await _factory.RunOutboxAsync();

        var entries = await admin.GetFromJsonAsync<List<AuditEntryResponse>>($"/api/{tenantId}/audit");

        Assert.Equal(4, entries!.Count);
        Assert.Contains(entries, e => e.EventType == "StrategyRevisionAddedEvent");
        Assert.Contains(entries, e => e.EventType == "StrategySubmittedEvent");
        Assert.Contains(entries, e => e.EventType == "StrategyApprovedEvent");
        Assert.Contains(entries, e => e.EventType == "StrategyImplementedEvent");

        // The trail says WHO did it, not just what happened.
        var submitted = entries.Single(e => e.EventType == "StrategySubmittedEvent");
        Assert.Equal(strategistUser, submitted.Data.GetProperty("SubmittedByUserId").GetGuid());
        var implemented = entries.Single(e => e.EventType == "StrategyImplementedEvent");
        Assert.Equal(editorUser, implemented.Data.GetProperty("ImplementedByUserId").GetGuid());

        var firstTwo = await admin.GetFromJsonAsync<List<AuditEntryResponse>>(
            $"/api/{tenantId}/audit?take=2");
        Assert.Equal(2, firstTwo!.Count);
    }

    [Fact]
    public async Task Only_Admins_Can_Read_The_Audit_Trail()
    {
        var tenantId = Guid.NewGuid();
        _factory.Access.AddMember(tenantId, "strategist-u2", Guid.NewGuid(), "Strategist");
        _factory.Access.AddMember(tenantId, "editor-u2", Guid.NewGuid(), "Editor");
        using var strategist = _factory.ClientFor("strategist-u2");
        using var editor = _factory.ClientFor("editor-u2");

        var asStrategist = await strategist.GetAsync($"/api/{tenantId}/audit");
        var asEditor = await editor.GetAsync($"/api/{tenantId}/audit");

        Assert.Equal(HttpStatusCode.Forbidden, asStrategist.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, asEditor.StatusCode);
    }

    [Fact]
    public async Task Another_Organizations_Admin_Cannot_See_The_Trail()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        _factory.Access.AddMember(tenantA, "admin-u3a", Guid.NewGuid(), "Admin");
        _factory.Access.AddMember(tenantB, "admin-u3b", Guid.NewGuid(), "Admin");

        var document = await _factory.SeedPendingDocumentAsync(tenantA);
        using var adminA = _factory.ClientFor("admin-u3a");
        await adminA.PostAsJsonAsync(
            $"/api/{tenantA}/strategies/{document.Id}/approve",
            new { revisionId = document.CurrentRevision!.Id });
        await _factory.RunOutboxAsync();

        using var adminB = _factory.ClientFor("admin-u3b");
        var ownTrail = await adminB.GetFromJsonAsync<List<AuditEntryResponse>>($"/api/{tenantB}/audit");
        var peeking = await adminB.GetAsync($"/api/{tenantA}/audit");

        Assert.Empty(ownTrail!);
        Assert.Equal(HttpStatusCode.NotFound, peeking.StatusCode);
    }
}