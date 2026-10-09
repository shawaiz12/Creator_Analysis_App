using CreatorAnalytics.Identity.Services;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Identity.Tests;

public class OnboardingServiceTests : IdentityDatabaseTestBase
{
    private async Task<CreatorAnalytics.Identity.Contracts.Services.OnboardingResult> Onboard(
        string externalId, string organizationName)
    {
        using var context = NewContext();
        return await new OnboardingService(context)
            .CreateOrganizationAsync(externalId, $"{externalId}@example.com", organizationName);
    }

    [Fact]
    public async Task First_Sign_In_Creates_User_Organization_And_Admin_Membership()
    {
        var result = await Onboard("ext-1", "Acme");

        Assert.True(result.Succeeded);

        using var context = NewContext();
        var access = await new TenantAccessService(context)
            .GetAccessAsync(result.OrganizationId, "ext-1");

        Assert.NotNull(access);
        Assert.Equal("Admin", access!.Role);
        Assert.Equal(result.UserId, access.UserId);
    }

    [Fact]
    public async Task A_Returning_User_Gets_A_Second_Organization_Without_A_Duplicate_User()
    {
        var first = await Onboard("ext-1", "Acme");
        var second = await Onboard("ext-1", "Globex");

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(first.UserId, second.UserId);
        Assert.NotEqual(first.OrganizationId, second.OrganizationId);

        using var context = NewContext();
        Assert.Equal(1, await context.Users.CountAsync());
        Assert.Equal(2, await context.Organizations.CountAsync());
    }

    [Fact]
    public async Task A_Blank_Name_Is_Refused_And_Nothing_Is_Saved()
    {
        var result = await Onboard("ext-1", "   ");

        Assert.False(result.Succeeded);

        using var context = NewContext();
        Assert.Equal(0, await context.Users.CountAsync());
        Assert.Equal(0, await context.Organizations.CountAsync());
    }

    [Fact]
    public async Task The_Organization_Limit_Is_Enforced()
    {
        for (var i = 1; i <= OnboardingService.MaxOrganizationsPerUser; i++)
            Assert.True((await Onboard("ext-1", $"Org {i}")).Succeeded);

        var tooMany = await Onboard("ext-1", "One too many");

        Assert.False(tooMany.Succeeded);

        using var context = NewContext();
        Assert.Equal(OnboardingService.MaxOrganizationsPerUser, await context.Organizations.CountAsync());
    }
}