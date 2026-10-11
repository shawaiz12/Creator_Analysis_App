using System.Text.Json;
using CreatorAnalytics.Audit.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CreatorAnalytics.Api.Endpoints;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        // Reading the audit trail: Admins of the organization only.
        app.MapGet("/api/{tenantId}/audit", async (
            [FromRoute] Guid tenantId,
            [FromServices] IAuditQueryService audit,
            int? take,
            int? skip) =>
        {
            var entries = await audit.ListAsync(tenantId, take ?? 50, skip ?? 0);

            return Results.Ok(entries.Select(e => new
            {
                e.Id,
                e.EventType,
                e.RecordedAtUtc,
                Data = ParseOrText(e.EventData)
            }));
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
    }

    private static object ParseOrText(string eventData)
    {
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(eventData);
        }
        catch (JsonException)
        {
            return eventData;
        }
    }
}