using System.Security.Claims;
using CreatorAnalytics.SharedKernel.Users;
using CreatorAnalytics.Strategy.Domain;
using CreatorAnalytics.Strategy.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using CreatorAnalytics.Integration.Contracts.Services;

namespace CreatorAnalytics.Strategy.Endpoints;

public sealed record AddRevisionRequest(string Content);

public sealed record ApproveRequest(Guid RevisionId);

public sealed record RejectRequest(Guid RevisionId, string Reason);

public sealed record RevisionDto(
    Guid Id, int VersionNumber, string Content, string Origin, Guid? AuthorUserId, DateTime CreatedAtUtc);

public sealed record ReviewDto(
    Guid Id, Guid RevisionId, Guid ReviewerUserId, string Decision, string? Reason, DateTime ReviewedAtUtc);

public sealed record StrategyDto(
    Guid Id,
    Guid VideoId,
    string Status,
    DateTime CreatedAtUtc,
    RevisionDto? CurrentRevision,
    IReadOnlyList<ReviewDto> Reviews);

public sealed record StrategySummaryDto(
    Guid Id, Guid VideoId, string Status, DateTime CreatedAtUtc, int RevisionCount);

public sealed record GenerationRequestDto(
    Guid Id, string Status, int Attempts, DateTime RequestedAtUtc, DateTime? CompletedAtUtc,
    string? Error, string? Model, string? PromptVersion);

public sealed record CreateStrategyRequest(Guid VideoId);

public static class StrategyEndpoints
{
    public static void MapStrategyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/{tenantId}/strategies")
                       .RequireAuthorization();

        // Create: Admins and Strategists only (Editors are read-only).
        // The video must exist in this organization.
        group.MapPost("/", async (
            [FromRoute] Guid tenantId,
            [FromBody] CreateStrategyRequest request,
            [FromServices] IVideoAnalyticsQuery videos,
            [FromServices] StrategyDbContext context) =>
        {
            var video = await videos.FindVideoAsync(request.VideoId);

            if (video is null)
                return Results.BadRequest(new { error = "That video does not exist in this organization." });

            var document = new StrategyDocument(tenantId, video.VideoId);

            context.Documents.Add(document);
            await context.SaveChangesAsync();

            return Results.Created(
                $"/api/{tenantId}/strategies/{document.Id}",
                new { DocumentId = document.Id });
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Strategist"));

        // List: every member, newest first. Editors only see Approved or Implemented strategies.
        group.MapGet("/", async (ClaimsPrincipal user, [FromServices] StrategyDbContext context) =>
        {
            var query = context.Documents.AsNoTracking();

            if (user.IsInRole("Editor"))
            {
                query = query.Where(d =>
                    d.Status == StrategyStatus.Approved || d.Status == StrategyStatus.Implemented);
            }

            var rows = await query
                .OrderByDescending(d => d.CreatedAtUtc)
                .Select(d => new { d.Id, d.VideoId, d.Status, d.CreatedAtUtc, RevisionCount = d.Revisions.Count() })
                .ToListAsync();

            return Results.Ok(rows.Select(r =>
                new StrategySummaryDto(r.Id, r.VideoId, r.Status.ToString(), r.CreatedAtUtc, r.RevisionCount)));
        });

        // Get one: every member, with the same Editor restriction.
        group.MapGet("/{id:guid}", async (
            [FromRoute] Guid id,
            ClaimsPrincipal user,
            [FromServices] StrategyDbContext context) =>
        {
            var document = await LoadAsync(context, id);

            if (document is null || !CanView(user, document))
                return Results.NotFound();

            return Results.Ok(ToDto(document));
        });

        // Add a revision: Admins and Strategists. Content must be editable (Draft or NeedsRevision).
        group.MapPost("/{id:guid}/revisions", async (
            [FromRoute] Guid id,
            [FromBody] AddRevisionRequest request,
            [FromServices] ICurrentUser currentUser,
            [FromServices] StrategyDbContext context) =>
        {
            if (currentUser.UserId is not Guid authorId)
                return Results.Unauthorized();

            return await Guard(async () =>
            {
                var document = await LoadAsync(context, id);
                if (document is null)
                    return Results.NotFound();

                var revision = document.AddRevision(request.Content, RevisionOrigin.Human, authorId);
                await context.SaveChangesAsync();

                return Results.Created($"/api/{document.TenantId}/strategies/{id}", ToDto(revision));
            });
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Strategist"));

        // Submit for approval: Admins and Strategists.
        group.MapPost("/{id:guid}/submit", async (
            [FromRoute] Guid id,
            [FromServices] ICurrentUser currentUser,
            [FromServices] StrategyDbContext context) =>
        {
            if (currentUser.UserId is not Guid submitterId)
                return Results.Unauthorized();

            return await Guard(async () =>
            {
                var document = await LoadAsync(context, id);
                if (document is null)
                    return Results.NotFound();

                document.SubmitForApproval(submitterId);
                await context.SaveChangesAsync();

                return Results.Ok(new { Status = document.Status.ToString() });
            });
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Strategist"));

        // Ask for an AI draft. The answer is immediate (202); the draft arrives a few seconds
        // later, written by the background worker.
        group.MapPost("/{id:guid}/generate", async (
            [FromRoute] Guid id,
            [FromServices] ICurrentUser currentUser,
            [FromServices] StrategyDbContext context) =>
        {
            if (currentUser.UserId is not Guid requesterId)
                return Results.Unauthorized();

            return await Guard(async () =>
            {
                var document = await LoadAsync(context, id);
                if (document is null)
                    return Results.NotFound();

                var request = document.RequestGeneration(requesterId, DateTime.UtcNow);
                await context.SaveChangesAsync();

                return Results.Accepted(
                    $"/api/{document.TenantId}/strategies/{id}/generation",
                    new { RequestId = request.Id });
            });
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Strategist"));

        // Where are my draft requests? Newest first.
        group.MapGet("/{id:guid}/generation", async (
            [FromRoute] Guid id,
            [FromServices] StrategyDbContext context) =>
        {
            var exists = await context.Documents.AnyAsync(d => d.Id == id);
            if (!exists)
                return Results.NotFound();

            var rows = await context.GenerationRequests
                .AsNoTracking()
                .Where(r => r.StrategyDocumentId == id)
                .OrderByDescending(r => r.RequestedAtUtc)
                .ToListAsync();

            return Results.Ok(rows.Select(r => new GenerationRequestDto(
                r.Id, r.Status.ToString(), r.Attempts, r.RequestedAtUtc, r.CompletedAtUtc,
                r.Error, r.Model, r.PromptVersion)));
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Strategist"));

        // Approve: Admins only. The client says which revision it reviewed.
        group.MapPost("/{id:guid}/approve", async (
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
        group.MapPost("/{id:guid}/reject", async (
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

        // Mark implemented: Editors and Admins. An Editor can only touch Approved strategies,
        // and for anything else it looks like the strategy does not exist.
        group.MapPost("/{id:guid}/implemented", async (
            [FromRoute] Guid id,
            ClaimsPrincipal user,
            [FromServices] ICurrentUser currentUser,
            [FromServices] StrategyDbContext context) =>
        {
            if (currentUser.UserId is not Guid implementerId)
                return Results.Unauthorized();

            return await Guard(async () =>
            {
                var document = await LoadAsync(context, id);
                if (document is null || !CanView(user, document))
                    return Results.NotFound();

                document.MarkImplemented(implementerId);
                await context.SaveChangesAsync();

                return Results.Ok(new { Status = document.Status.ToString() });
            });
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Editor"));
    }

    private static Task<StrategyDocument?> LoadAsync(StrategyDbContext context, Guid id) =>
     context.Documents
         .Include(d => d.Revisions)
         .Include(d => d.Reviews)
         .Include(d => d.GenerationRequests)
         .SingleOrDefaultAsync(d => d.Id == id);

    // Editors may only see strategies that have been approved.
    private static bool CanView(ClaimsPrincipal user, StrategyDocument document) =>
        !user.IsInRole("Editor")
        || document.Status is StrategyStatus.Approved or StrategyStatus.Implemented;

    private static RevisionDto ToDto(StrategyRevision r) =>
        new(r.Id, r.VersionNumber, r.Content, r.Origin.ToString(), r.AuthorUserId, r.CreatedAtUtc);

    private static ReviewDto ToDto(StrategyReview r) =>
        new(r.Id, r.RevisionId, r.ReviewerUserId, r.Decision.ToString(), r.Reason, r.ReviewedAtUtc);

    private static StrategyDto ToDto(StrategyDocument d) =>
        new(d.Id,
            d.VideoId,
            d.Status.ToString(),
            d.CreatedAtUtc,
            d.CurrentRevision is null ? null : ToDto(d.CurrentRevision),
            d.Reviews.OrderBy(r => r.ReviewedAtUtc).Select(ToDto).ToList());

    // Turns expected failures into proper HTTP answers instead of 500 errors.
    // TenantViolationException is deliberately NOT caught: that is a bug or an attack.
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
        catch (DbUpdateException)
        {
            return Results.Conflict(new { error = "The change conflicted with another update. Reload and try again." });
        }
    }
}