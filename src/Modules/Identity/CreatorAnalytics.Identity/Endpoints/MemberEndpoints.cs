using CreatorAnalytics.Identity.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CreatorAnalytics.Identity.Endpoints;

internal static class MemberEndpoints
{
    internal static void MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        // Who is in this organization: Admins only.
        // The gatekeeper has already proven the caller belongs to {tenantId}.
        app.MapGet("/api/{tenantId}/members", async (
            [FromRoute] Guid tenantId,
            [FromServices] IMembershipQueryService memberships) =>
        {
            return Results.Ok(await memberships.ListMembersAsync(tenantId));
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}