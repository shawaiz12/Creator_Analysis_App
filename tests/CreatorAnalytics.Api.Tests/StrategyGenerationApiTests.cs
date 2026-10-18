using System.Net;
using System.Net.Http.Json;
using CreatorAnalytics.Strategy.Endpoints;

namespace CreatorAnalytics.Api.Tests;

public class StrategyGenerationApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly Guid _tenantId = Guid.NewGuid();
    private string Base => $"/api/{_tenantId}/strategies";

    public StrategyGenerationApiTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Access.AddMember(_tenantId, "admin-g", Guid.NewGuid(), "Admin");
        _factory.Access.AddMember(_tenantId, "strategist-g", Guid.NewGuid(), "Strategist");
        _factory.Access.AddMember(_tenantId, "editor-g", Guid.NewGuid(), "Editor");
    }

    private async Task<Guid> CreateStrategyAsync(HttpClient client, Guid videoId)
    {
        var created = await client.PostAsJsonAsync(Base, new { videoId });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.DocumentId;
    }

    [Fact]
    public async Task A_Strategist_Requests_A_Draft_And_The_Worker_Delivers_It()
    {
        var videoId = await _factory.SeedVideoAsync(
            _tenantId, withRetention: true, transcript: "Hello and welcome to the channel.");
        using var strategist = _factory.ClientFor("strategist-g");
        var id = await CreateStrategyAsync(strategist, videoId);

        var requested = await strategist.PostAsync($"{Base}/{id}/generate", null);
        Assert.Equal(HttpStatusCode.Accepted, requested.StatusCode);

        await _factory.RunGenerationAsync();

        var strategy = await strategist.GetFromJsonAsync<StrategyDto>($"{Base}/{id}");
        Assert.Equal("Ai", strategy!.CurrentRevision!.Origin);
        Assert.Null(strategy.CurrentRevision.AuthorUserId);
        Assert.Contains("Draft strategy for", strategy.CurrentRevision.Content);

        var requests = await strategist.GetFromJsonAsync<List<GenerationRequestDto>>($"{Base}/{id}/generation");
        Assert.Equal("Succeeded", Assert.Single(requests!).Status);
    }

    [Fact]
    public async Task Only_One_Request_Can_Be_Open_At_A_Time()
    {
        var videoId = await _factory.SeedVideoAsync(_tenantId, withRetention: true);
        using var strategist = _factory.ClientFor("strategist-g");
        var id = await CreateStrategyAsync(strategist, videoId);

        var first = await strategist.PostAsync($"{Base}/{id}/generate", null);
        var second = await strategist.PostAsync($"{Base}/{id}/generate", null);

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Editors_Can_Neither_Request_Nor_See_Draft_Requests()
    {
        var document = await _factory.SeedPendingDocumentAsync(_tenantId);
        using var editor = _factory.ClientFor("editor-g");

        var request = await editor.PostAsync($"{Base}/{document.Id}/generate", null);
        var status = await editor.GetAsync($"{Base}/{document.Id}/generation");

        Assert.Equal(HttpStatusCode.Forbidden, request.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, status.StatusCode);
    }

    [Fact]
    public async Task A_Video_Without_Retention_Fails_With_A_Helpful_Message()
    {
        var videoId = await _factory.SeedVideoAsync(_tenantId);
        using var strategist = _factory.ClientFor("strategist-g");
        var id = await CreateStrategyAsync(strategist, videoId);

        await strategist.PostAsync($"{Base}/{id}/generate", null);
        await _factory.RunGenerationAsync();

        var requests = await strategist.GetFromJsonAsync<List<GenerationRequestDto>>($"{Base}/{id}/generation");
        var request = Assert.Single(requests!);
        Assert.Equal("Failed", request.Status);
        Assert.Contains("Sync the channel", request.Error);
    }

    [Fact]
    public async Task A_Strategy_Under_Review_Cannot_Request_A_Draft()
    {
        var document = await _factory.SeedPendingDocumentAsync(_tenantId);
        using var strategist = _factory.ClientFor("strategist-g");

        var response = await strategist.PostAsync($"{Base}/{document.Id}/generate", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Another_Organizations_Admin_Cannot_Request_Or_Read_Drafts()
    {
        var tenantB = Guid.NewGuid();
        _factory.Access.AddMember(tenantB, "admin-g-b", Guid.NewGuid(), "Admin");
        var document = await _factory.SeedPendingDocumentAsync(_tenantId);
        using var adminB = _factory.ClientFor("admin-g-b");

        var request = await adminB.PostAsync($"/api/{tenantB}/strategies/{document.Id}/generate", null);
        var status = await adminB.GetAsync($"/api/{tenantB}/strategies/{document.Id}/generation");

        Assert.Equal(HttpStatusCode.NotFound, request.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, status.StatusCode);
    }

    [Fact]
    public async Task Asking_For_A_Draft_And_Receiving_One_Are_Both_Audited()
    {
        var videoId = await _factory.SeedVideoAsync(_tenantId, withRetention: true);
        using var strategist = _factory.ClientFor("strategist-g");
        using var admin = _factory.ClientFor("admin-g");
        var id = await CreateStrategyAsync(strategist, videoId);

        await strategist.PostAsync($"{Base}/{id}/generate", null);
        await _factory.RunGenerationAsync();
        await _factory.RunOutboxAsync();

        var entries = await admin.GetFromJsonAsync<List<AuditEntryResponse>>($"/api/{_tenantId}/audit");

        Assert.Contains(entries!, e => e.EventType == "StrategyGenerationRequestedEvent");
        Assert.Contains(entries!, e => e.EventType == "StrategyRevisionAddedEvent");
    }
}