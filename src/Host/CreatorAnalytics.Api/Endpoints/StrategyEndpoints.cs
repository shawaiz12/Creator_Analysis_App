using CreatorAnalytics.SharedKernel.Users;
using CreatorAnalytics.Strategy.Domain;
using CreatorAnalytics.Strategy.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Api.Endpoints;

public sealed record ApproveRequest(Guid RevisionId);

public sealed record RejectRequest(Guid RevisionId, string Reason);

public static class StrategyEndpoints
{
    public static void MapStrategyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/{tenantId}/strategies")
                       .RequireAuthorization();

        // Create: Admins and Strategists only (Editors are read-only).
        group.MapPost("/", async ([FromRoute] Guid tenantId, [FromServices] StrategyDbContext context) =>
        {
            var videoId = Guid.NewGuid(); // Placeholder until the Integration module provides real videos
            var document = new StrategyDocument(tenantId, videoId);

            context.Documents.Add(document);
            await context.SaveChangesAsync();

            return Results.Created(
                $"/api/{tenantId}/strategies/{document.Id}",
                new { DocumentId = document.Id });
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Strategist"));

        // Approve: Admins only. The client says which revision it reviewed.
        group.MapPost("/{id}/approve", async (
            [FromRoute] Guid id,
            [FromBody] ApproveRequest request,
            [FromServices] ICurrentUser currentUser,
            [FromServices] StrategyDbContext context) =>
        {
            if (currentUser.UserId is not Guid reviewerId)
                return Results.Unauthorized();

            return await Guard(async () =>
            {
                var document = await LoadAsync(context, id);
                if (document is null)
                    return Results.NotFound();

                document.Approve(reviewerId, request.RevisionId);
                await context.SaveChangesAsync();

                return Results.Ok(new { Status = document.Status.ToString() });
            });
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        // Reject: Admins only, with a mandatory reason.
        group.MapPost("/{id}/reject", async (
            [FromRoute] Guid id,
            [FromBody] RejectRequest request,
            [FromServices] ICurrentUser currentUser,
            [FromServices] StrategyDbContext context) =>
        {
            if (currentUser.UserId is not Guid reviewerId)
                return Results.Unauthorized();

            return await Guard(async () =>
            {
                var document = await LoadAsync(context, id);
                if (document is null)
                    return Results.NotFound();

                document.Reject(reviewerId, request.RevisionId, request.Reason);
                await context.SaveChangesAsync();

                return Results.Ok(new { Status = document.Status.ToString() });
            });
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
    }

    private static Task<StrategyDocument?> LoadAsync(StrategyDbContext context, Guid id) =>
        context.Documents
            .Include(d => d.Revisions)
            .Include(d => d.Reviews)
            .SingleOrDefaultAsync(d => d.Id == id);

    // Turns expected failures into proper HTTP answers instead of 500 errors.
    private static async Task<IResult> Guard(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { error = "The strategy was changed by someone else. Reload and try again." });
        }
    }
}