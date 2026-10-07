using System;
using CreatorAnalytics.Strategy.Domain;
using CreatorAnalytics.Strategy.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CreatorAnalytics.Api.Endpoints;

public static class StrategyEndpoints
{
    public static void MapStrategyEndpoints(this IEndpointRouteBuilder app)
    {
        // Every route in this group starts with the tenant ID and requires a valid JWT
        var group = app.MapGroup("/api/{tenantId}/strategies")
                       .RequireAuthorization();

        // 1. Create a new strategy document (Accessible by any tenant member)
        group.MapPost("/", async ([FromRoute] Guid tenantId, [FromServices] StrategyDbContext context) =>
        {
            // The Gatekeeper middleware already verified the user belongs to this tenant,
            // and the DB Context is already locked to this tenant ID.
            var videoId = Guid.NewGuid(); // Placeholder for the Integration module's Video ID
            var document = new StrategyDocument(tenantId, videoId);

            context.Documents.Add(document);
            await context.SaveChangesAsync();

            return Results.Ok(new { DocumentId = document.Id });
        });

        // 2. Approve a strategy (Strictly restricted to Admins)
        group.MapPost("/{id}/approve", async (
            [FromRoute] Guid tenantId,
            [FromRoute] Guid id,
            [FromServices] StrategyDbContext context) =>
        {
            var document = await context.Documents
                .Include(d => d.Revisions)
                .Include(d => d.Reviews)
                .SingleOrDefaultAsync(d => d.Id == id);

            if (document == null)
                return Results.NotFound();

            // In a complete flow, we would extract the specific user's ID from the JWT claims here
            var reviewerId = Guid.NewGuid();
            var currentRevisionId = document.CurrentRevision?.Id ?? Guid.Empty;

            // This will throw a domain exception if the document isn't in the 'Pending Approval' state
            document.Approve(reviewerId, currentRevisionId);

            await context.SaveChangesAsync();
            return Results.Ok(new { Status = document.Status.ToString() });

        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}