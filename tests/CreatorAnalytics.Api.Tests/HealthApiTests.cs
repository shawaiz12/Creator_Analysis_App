using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using CreatorAnalytics.Api.Health;

namespace CreatorAnalytics.Api.Tests;

public class HealthApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public HealthApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Liveness_Needs_No_Sign_In()
    {
        using var anonymous = _factory.ClientFor(null);

        var response = await anonymous.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_Is_Healthy_When_The_Database_Answers()
    {
        using var anonymous = _factory.ClientFor(null);

        var response = await anonymous.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task An_Unreachable_Database_Is_Unhealthy_And_The_Answer_Hides_The_Details()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    "Server=localhost,1;Database=x;User Id=sa;Password=Super_Secret_1;Connect Timeout=2;TrustServerCertificate=True"
            })
            .Build();

        var result = await new DatabaseHealthCheck(configuration).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.DoesNotContain("Super_Secret_1", result.Description);
        Assert.DoesNotContain("localhost", result.Description);
    }

    [Fact]
    public async Task A_Missing_Connection_String_Is_Unhealthy()
    {
        var configuration = new ConfigurationBuilder().Build();

        var result = await new DatabaseHealthCheck(configuration).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }
}