using System.Net;
using System.Net.Http.Json;
using CreatorAnalytics.Strategy.Domain;

namespace CreatorAnalytics.Api.Tests;

public class StrategyApprovalApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();
    private readonly Guid _adminAUserId = Guid.NewGuid();

    public StrategyApprovalApiTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Access.AddMember(_tenantA, "admin-a", _adminAUserId, "Admin");
        _factory.Access.AddMember(_tenantA, "strategist-a", Guid.NewGuid(), "Strategist");
        _factory.Access.AddMember(_tenantA, "editor-a", Guid.NewGuid(), "Editor");
        _factory.Access.AddMember(_tenantB, "admin-b", Guid.NewGuid(), "Admin");
    }

    [Fact]
    public async Task Admin_Approves_And_The_Real_Reviewer_Is_Recorded()
    {
        var document = await _factory.SeedPendingDocumentAsync(_tenantA);
        using var client = _factory.ClientFor("admin-a");

        var response = await client.PostAsJsonAsync(
            $"/api/{_tenantA}/strategies/{document.Id}/approve",
            new { revisionId = document.CurrentRevision!.Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await _factory.GetDocumentAsync(_tenantA, document.Id);
        Assert.Equal(StrategyStatus.Approved, saved.Status);
        var review = Assert.Single(saved.Reviews);
        Assert.Equal(_adminAUserId, review.ReviewerUserId);
    }

    [Fact]
    public async Task Approving_A_Stale_Revision_Returns_409()
    {
        var document = await _factory.SeedPendingDocumentAsync(_tenantA);
        using var client = _factory.ClientFor("admin-a");

        var response = await client.PostAsJsonAsync(
            $"/api/{_tenantA}/strategies/{document.Id}/approve",
            new { revisionId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var saved = await _factory.GetDocumentAsync(_tenantA, document.Id);
        Assert.Equal(StrategyStatus.PendingApproval, saved.Status);
    }

    [Fact]
    public async Task Approving_Twice_Returns_409_The_Second_Time()
    {
        var document = await _factory.SeedPendingDocumentAsync(_tenantA);
        using var client = _factory.ClientFor("admin-a");
        var body = new { revisionId = document.CurrentRevision!.Id };
        var url = $"/api/{_tenantA}/strategies/{document.Id}/approve";

        var first = await client.PostAsJsonAsync(url, body);
        var second = await client.PostAsJsonAsync(url, body);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task A_Strategist_Cannot_Approve()
    {
        var document = await _factory.SeedPendingDocumentAsync(_tenantA);
        using var client = _factory.ClientFor("strategist-a");

        var response = await client.PostAsJsonAsync(
            $"/api/{_tenantA}/strategies/{document.Id}/approve",
            new { revisionId = document.CurrentRevision!.Id });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var saved = await _factory.GetDocumentAsync(_tenantA, document.Id);
        Assert.Equal(StrategyStatus.PendingApproval, saved.Status);
    }

    [Fact]
    public async Task An_Editor_Cannot_Create_A_Strategy()
    {
        using var client = _factory.ClientFor("editor-a");

        var response = await client.PostAsync($"/api/{_tenantA}/strategies", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_Admin_Of_Another_Tenant_Cannot_Touch_The_Document()
    {
        var document = await _factory.SeedPendingDocumentAsync(_tenantA);
        using var client = _factory.ClientFor("admin-b");

        // Admin B uses their own tenant in the URL but guesses Tenant A's document id.
        var response = await client.PostAsJsonAsync(
            $"/api/{_tenantB}/strategies/{document.Id}/approve",
            new { revisionId = document.CurrentRevision!.Id });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var saved = await _factory.GetDocumentAsync(_tenantA, document.Id);
        Assert.Equal(StrategyStatus.PendingApproval, saved.Status);
    }

    [Fact]
    public async Task Admin_Rejects_With_A_Reason_And_It_Is_Stored()
    {
        var document = await _factory.SeedPendingDocumentAsync(_tenantA);
        using var client = _factory.ClientFor("admin-a");

        var response = await client.PostAsJsonAsync(
            $"/api/{_tenantA}/strategies/{document.Id}/reject",
            new { revisionId = document.CurrentRevision!.Id, reason = "Hook is weak." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await _factory.GetDocumentAsync(_tenantA, document.Id);
        Assert.Equal(StrategyStatus.NeedsRevision, saved.Status);
        var review = Assert.Single(saved.Reviews);
        Assert.Equal("Hook is weak.", review.Reason);
    }

    [Fact]
    public async Task Rejecting_Without_A_Reason_Returns_400()
    {
        var document = await _factory.SeedPendingDocumentAsync(_tenantA);
        using var client = _factory.ClientFor("admin-a");

        var response = await client.PostAsJsonAsync(
            $"/api/{_tenantA}/strategies/{document.Id}/reject",
            new { revisionId = document.CurrentRevision!.Id, reason = "  " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}