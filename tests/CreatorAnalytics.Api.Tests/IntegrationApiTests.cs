using System.Net;
using System.Net.Http.Json;
using CreatorAnalytics.Integration.Endpoints;

namespace CreatorAnalytics.Api.Tests;

public sealed record ChannelCreatedResponse(Guid ChannelId);

public class IntegrationApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly Guid _tenantId = Guid.NewGuid();
    private string Base => $"/api/{_tenantId}";

    public IntegrationApiTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Access.AddMember(_tenantId, "admin-i", Guid.NewGuid(), "Admin");
        _factory.Access.AddMember(_tenantId, "strategist-i", Guid.NewGuid(), "Strategist");
        _factory.Access.AddMember(_tenantId, "editor-i", Guid.NewGuid(), "Editor");
    }

    private async Task<Guid> ConnectAsync(HttpClient admin, string channelId = "UC-demo")
    {
        var response = await admin.PostAsJsonAsync(
            $"{Base}/channels", new { youTubeChannelId = channelId, title = "Studio" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ChannelCreatedResponse>())!.ChannelId;
    }

    [Fact]
    public async Task An_Admin_Connects_A_Channel_And_A_Strategist_Can_List_It()
    {
        using var admin = _factory.ClientFor("admin-i");
        using var strategist = _factory.ClientFor("strategist-i");
        var id = await ConnectAsync(admin);

        var list = await strategist.GetFromJsonAsync<List<ChannelDto>>($"{Base}/channels");

        var channel = Assert.Single(list!);
        Assert.Equal(id, channel.Id);
        Assert.Equal("Connected", channel.Status);
        Assert.Null(channel.LastSyncedAtUtc);
    }

    [Fact]
    public async Task Connecting_The_Same_Channel_Twice_Returns_409()
    {
        using var admin = _factory.ClientFor("admin-i");
        await ConnectAsync(admin);

        var second = await admin.PostAsJsonAsync(
            $"{Base}/channels", new { youTubeChannelId = "UC-demo", title = "Again" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Only_Admins_Can_Connect_A_Channel()
    {
        using var strategist = _factory.ClientFor("strategist-i");

        var response = await strategist.PostAsJsonAsync(
            $"{Base}/channels", new { youTubeChannelId = "UC-x", title = "Nope" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Editors_Cannot_See_Channels_Or_Videos()
    {
        using var editor = _factory.ClientFor("editor-i");

        var channels = await editor.GetAsync($"{Base}/channels");
        var videos = await editor.GetAsync($"{Base}/videos");

        Assert.Equal(HttpStatusCode.Forbidden, channels.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, videos.StatusCode);
    }

    [Fact]
    public async Task A_Sync_Fills_The_Videos_And_Their_Retention()
    {
        using var admin = _factory.ClientFor("admin-i");
        using var strategist = _factory.ClientFor("strategist-i");
        var channelId = await ConnectAsync(admin);

        var sync = await strategist.PostAsync($"{Base}/channels/{channelId}/sync", null);

        Assert.Equal(HttpStatusCode.OK, sync.StatusCode);
        var summary = await sync.Content.ReadFromJsonAsync<SyncResponse>();
        Assert.Equal(3, summary!.VideosAdded);

        var videos = await strategist.GetFromJsonAsync<List<VideoDto>>($"{Base}/videos");
        Assert.Equal(3, videos!.Count);
        Assert.All(videos, v => Assert.False(v.HasTranscript));
        Assert.Equal("Thumbnail ideas that work", videos[0].Title); // newest first

        var retention = await strategist.GetFromJsonAsync<RetentionDto>(
            $"{Base}/videos/{videos[0].Id}/retention");
        Assert.Equal(100, retention!.Points.Count);
    }

    [Fact]
    public async Task A_Transcript_Can_Be_Saved_And_Is_Reported()
    {
        using var admin = _factory.ClientFor("admin-i");
        using var strategist = _factory.ClientFor("strategist-i");
        var channelId = await ConnectAsync(admin);
        await strategist.PostAsync($"{Base}/channels/{channelId}/sync", null);
        var videos = await strategist.GetFromJsonAsync<List<VideoDto>>($"{Base}/videos");
        var videoId = videos![0].Id;

        var blank = await strategist.PutAsJsonAsync(
            $"{Base}/videos/{videoId}/transcript", new { transcript = "   " });
        var saved = await strategist.PutAsJsonAsync(
            $"{Base}/videos/{videoId}/transcript", new { transcript = "Hello and welcome." });

        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);

        var after = await strategist.GetFromJsonAsync<List<VideoDto>>($"{Base}/videos");
        Assert.True(after!.Single(v => v.Id == videoId).HasTranscript);
    }

    [Fact]
    public async Task Another_Tenants_Admin_Cannot_Sync_Or_Read_Retention()
    {
        var tenantB = Guid.NewGuid();
        _factory.Access.AddMember(tenantB, "admin-i-b", Guid.NewGuid(), "Admin");
        using var admin = _factory.ClientFor("admin-i");
        using var strategist = _factory.ClientFor("strategist-i");
        using var adminB = _factory.ClientFor("admin-i-b");

        var channelId = await ConnectAsync(admin);
        await strategist.PostAsync($"{Base}/channels/{channelId}/sync", null);
        var videos = await strategist.GetFromJsonAsync<List<VideoDto>>($"{Base}/videos");

        var sync = await adminB.PostAsync($"/api/{tenantB}/channels/{channelId}/sync", null);
        var retention = await adminB.GetAsync($"/api/{tenantB}/videos/{videos![0].Id}/retention");

        Assert.Equal(HttpStatusCode.NotFound, sync.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, retention.StatusCode);
    }
}