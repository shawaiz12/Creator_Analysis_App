using Microsoft.AspNetCore.Routing;

namespace CreatorAnalytics.Identity.Endpoints;

public static class IdentityEndpoints
{
    public static void MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapOnboardingEndpoints();
        app.MapInvitationEndpoints();
        app.MapMeEndpoints();
        app.MapMemberEndpoints();
    }
}