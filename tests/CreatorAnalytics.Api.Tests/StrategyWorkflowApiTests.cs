using System.Net;
using System.Net.Http.Json;
using CreatorAnalytics.Api.Endpoints;

namespace CreatorAnalytics.Api.Tests;

public sealed record CreatedResponse(Guid DocumentId);

public class StrategyWorkflowApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly Guid _tenantId = Guid.NewGuid();
    private string BaseUrl => $"/api/{_tenantId}/strategies";

    public StrategyWorkflowApiTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Access.AddMember(_tenantId, "admin-w", Guid.NewGuid(), "Admin");
        _factory.Access.AddMember(_tenantId, "strategist-w", Guid.NewGuid(), "Strategist");
        _factory.Access.AddMember(_tenantId, "editor-w", Guid.NewGuid(), "Editor");
    }

    [Fact]
    public async Task A_Strategy_Travels_The_Whole_Lifecycle_Through_The_Api()
    {
        using var strategist = _factory.ClientFor("strategist-w");
        using var admin = _factory.ClientFor("admin-w");
        using var editor = _factory.ClientFor("editor-w");

        var created = await strategist.PostAsync(BaseUrl, null);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.DocumentId;

        var revisionResponse = await strategist.PostAsJsonAsync(
            $"{BaseUrl}/{id}/revisions",
            new { content = "Open with the result, then tell the story." });
        Assert.Equal(HttpStatusCode.Created, revisionResponse.StatusCode);
        var revision = (await revisionResponse.Content.ReadFromJsonAsync<RevisionDto>())!;
        Assert.Equal(1, revision.VersionNumber);

        var submitted = await strategist.PostAsync($"{BaseUrl}/{id}/submit", null);
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);

        // Editors cannot even see a strategy that is still under review.
        var hidden = await editor.GetAsync($"{BaseUrl}/{id}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        var approved = await admin.PostAsJsonAsync(
            $"{BaseUrl}/{id}/approve", new { revisionId = revision.Id });
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);

        var visible = await editor.GetAsync($"{BaseUrl}/{id}");
        Assert.Equal(HttpStatusCode.OK, visible.StatusCode);

        var implemented = await editor.PostAsync($"{BaseUrl}/{id}/implemented", null);
        Assert.Equal(HttpStatusCode.OK, implemented.StatusCode);

        var final = await admin.GetFromJsonAsync<StrategyDto>($"{BaseUrl}/{id}");
        Assert.Equal("Implemented", final!.Status);
        Assert.Single(final.Reviews);
    }

    [Fact]
    public async Task An_Editor_Cannot_Implement_A_Strategy_That_Is_Not_Approved()
    {
        var document = await _factory.SeedPendingDocumentAsync(_tenantId);
        using var editor = _factory.ClientFor("editor-w");

        var response = await editor.PostAsync($"{BaseUrl}/{document.Id}/implemented", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task An_Editor_Only_Sees_Approved_Strategies_In_The_List()
    {
        var pending = await _factory.SeedPendingDocumentAsync(_tenantId);
        var toApprove = await _factory.SeedPendingDocumentAsync(_tenantId);
        using var admin = _factory.ClientFor("admin-w");
        using var editor = _factory.ClientFor("editor-w");

        var approved = await admin.PostAsJsonAsync(
            $"{BaseUrl}/{toApprove.Id}/approve",
            new { revisionId = toApprove.CurrentRevision!.Id });
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);

        var list = await editor.GetFromJsonAsync<List<StrategySummaryDto>>(BaseUrl);

        Assert.Contains(list!, s => s.Id == toApprove.Id);
        Assert.DoesNotContain(list!, s => s.Id == pending.Id);
    }

    [Fact]
    public async Task An_Editor_Cannot_Add_Revisions_Or_Submit()
    {
        var document = await _factory.SeedPendingDocumentAsync(_tenantId);
        using var editor = _factory.ClientFor("editor-w");

        var revision = await editor.PostAsJsonAsync(
            $"{BaseUrl}/{document.Id}/revisions", new { content = "Sneaky edit" });
        var submit = await editor.PostAsync($"{BaseUrl}/{document.Id}/submit", null);

        Assert.Equal(HttpStatusCode.Forbidden, revision.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, submit.StatusCode);
    }

    [Fact]
    public async Task Content_Is_Frozen_While_A_Strategy_Is_Under_Review()
    {
        var document = await _factory.SeedPendingDocumentAsync(_tenantId);
        using var admin = _factory.ClientFor("admin-w");

        var response = await admin.PostAsJsonAsync(
            $"{BaseUrl}/{document.Id}/revisions", new { content = "Late edit" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}