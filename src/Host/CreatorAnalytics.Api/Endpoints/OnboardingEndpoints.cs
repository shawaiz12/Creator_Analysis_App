using System.Security.Claims;
using CreatorAnalytics.Api.Security;
using CreatorAnalytics.Identity.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Api.Endpoints;

public sealed record CreateOrganizationRequest(string Name);

public static class OnboardingEndpoints
{
    public static void MapOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        // Outside the tenant route on purpose: a brand-new user has no tenant yet.
        app.MapPost("/api/onboarding/organizations", async (
            [FromBody] CreateOrganizationRequest request,
            ClaimsPrincipal user,
            [FromServices] IOnboardingService onboarding) =>
        {
            var externalId = user.GetExternalUserId();
            var email = user.GetEmail();

            if (string.IsNullOrWhiteSpace(externalId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(email))
                return Results.BadRequest(new { error = "Your sign-in token has no email address." });

            try
            {
                var result = await onboarding.CreateOrganizationAsync(externalId, email, request.Name);

                return result.Succeeded
                    ? Results.Created(
                        $"/api/{result.OrganizationId}/strategies",
                        new { OrganizationId = result.OrganizationId })
                    : Results.BadRequest(new { error = result.Error });
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new
                {
                    error = "Your account was being created at the same moment. Please try again."
                });
            }
        }).RequireAuthorization();
    }
}