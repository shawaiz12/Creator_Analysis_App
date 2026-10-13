using System.Security.Claims;
using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.SharedKernel.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CreatorAnalytics.Identity.Endpoints;

internal static class MeEndpoints
{
    internal static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        // Outside the tenant route: it answers "which organizations am I in?"
        app.MapGet("/api/me", async (
            ClaimsPrincipal user,
            [FromServices] IMembershipQueryService memberships) =>
        {
            var externalId = user.GetExternalUserId();

            if (string.IsNullOrWhiteSpace(externalId))
                return Results.Unauthorized();

            var organizations = await memberships.ListForUserAsync(externalId);

            return Results.Ok(new { Email = user.GetEmail(), Organizations = organizations });
        }).RequireAuthorization();
    }
}