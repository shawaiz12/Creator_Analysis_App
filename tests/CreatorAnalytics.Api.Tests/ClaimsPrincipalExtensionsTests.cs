using System.Security.Claims;
using CreatorAnalytics.SharedKernel.Security;

namespace CreatorAnalytics.Api.Tests;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal PrincipalWith(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "Test"));

    [Fact]
    public void The_Stable_Oid_Claim_Wins_Over_Sub()
    {
        var user = PrincipalWith(("sub", "pairwise-id"), ("oid", "stable-user-id"));

        Assert.Equal("stable-user-id", user.GetExternalUserId());
    }

    [Fact]
    public void Sub_Is_The_Fallback_When_There_Is_No_Oid()
    {
        var user = PrincipalWith(("sub", "pairwise-id"));

        Assert.Equal("pairwise-id", user.GetExternalUserId());
    }

    [Fact]
    public void An_Email_Claim_Wins_Over_The_Sign_In_Name()
    {
        var user = PrincipalWith(("email", "ana@example.com"), ("preferred_username", "ana_ext#EXT#@tenant"));

        Assert.Equal("ana@example.com", user.GetEmail());
    }

    [Fact]
    public void The_Sign_In_Name_Is_Used_When_There_Is_No_Email_Claim()
    {
        var user = PrincipalWith(("preferred_username", "shawaiz@example.com"));

        Assert.Equal("shawaiz@example.com", user.GetEmail());
    }
}