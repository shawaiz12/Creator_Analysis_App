using System.Security.Claims;
using CreatorAnalytics.Api.Security;
using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CreatorAnalytics.Api.Endpoints;

public sealed record CreateInvitationRequest(string Email, string Role);

public static class InvitationEndpoints
{
    public static void MapInvitationEndpoints(this IEndpointRouteBuilder app)
    {
        // Managing invitations: Admins of the organization only.
        var group = app.MapGroup("/api/{tenantId}/invitations")
                       .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapPost("/", async (
            [FromRoute] Guid tenantId,
            [FromBody] CreateInvitationRequest request,
            [FromServices] ICurrentUser currentUser,
            [FromServices] IInvitationService invitations) =>
        {
            if (currentUser.UserId is not Guid inviterId)
                return Results.Unauthorized();

            var result = await invitations.CreateAsync(
                tenantId, inviterId, request.Email, request.Role, DateTime.UtcNow);

            return ToResult(result, () => Results.Created(
                $"/api/{tenantId}/invitations",
                new { InvitationId = result.InvitationId }));
        });

        group.MapGet("/", async (
            [FromRoute] Guid tenantId,
            [FromServices] IInvitationService invitations) =>
        {
            return Results.Ok(await invitations.ListAsync(tenantId, DateTime.UtcNow));
        });

        group.MapPost("/{id:guid}/revoke", async (
            [FromRoute] Guid tenantId,
            [FromRoute] Guid id,
            [FromServices] IInvitationService invitations) =>
        {
            var result = await invitations.RevokeAsync(tenantId, id);
            return ToResult(result, () => Results.Ok(new { Status = "Revoked" }));
        });

        // Accepting: outside the tenant route, because the invited person is not a member yet.
        // Any signed-in user may try; only the invited email can succeed.
        app.MapPost("/api/invitations/{id:guid}/accept", async (
            [FromRoute] Guid id,
            ClaimsPrincipal user,
            [FromServices] IInvitationService invitations) =>
        {
            var externalId = user.GetExternalUserId();
            var email = user.GetEmail();

            if (string.IsNullOrWhiteSpace(externalId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(email))
                return Results.BadRequest(new { error = "Your sign-in token has no email address." });

            var result = await invitations.AcceptAsync(id, externalId, email, DateTime.UtcNow);

            return ToResult(result, () => Results.Ok(new { OrganizationId = result.OrganizationId }));
        }).RequireAuthorization();
    }

    private static IResult ToResult(InvitationResult result, Func<IResult> onSuccess) =>
        result.Outcome switch
        {
            InvitationOutcome.Succeeded => onSuccess(),
            InvitationOutcome.NotFound => Results.NotFound(),
            InvitationOutcome.Conflict => Results.Conflict(new { error = result.Error }),
            _ => Results.BadRequest(new { error = result.Error })
        };
}