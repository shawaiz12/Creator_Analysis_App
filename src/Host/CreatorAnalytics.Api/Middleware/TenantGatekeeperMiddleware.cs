using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CreatorAnalytics.Api.Middleware;

public class TenantGatekeeperMiddleware
{
    private readonly RequestDelegate _next;

    public TenantGatekeeperMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    // Scoped services (like ITenantAccessService and TenantContext) are injected into InvokeAsync
    public async Task InvokeAsync(HttpContext context, ITenantAccessService accessService, TenantContext tenantContext)
    {
        var tenantRouteValue = context.GetRouteValue("tenantId")?.ToString();

        // If there's no tenantId in the URL, this isn't a tenant-specific endpoint. Let it pass.
        if (string.IsNullOrEmpty(tenantRouteValue) || !Guid.TryParse(tenantRouteValue, out var tenantId))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity is null || !context.User.Identity.IsAuthenticated)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Microsoft Entra ID usually puts the user's unique ID in the Object Identifier claim or the 'sub' claim
        var externalUserId = context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                             ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? context.User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(externalUserId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Ask the Identity module if this user is a member of this tenant
        var role = await accessService.GetUserRoleAsync(tenantId, externalUserId);

        if (role == null)
        {
            // The user is not a member. Return 404 Not Found to completely hide the tenant's existence.
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // Lock the tenant into the request context so EF Core Global Query Filters can use it
        tenantContext.Set(tenantId);

        // Add the role as a temporary claim for this specific request so standard [Authorize(Roles="Admin")] attributes work
        var identity = (ClaimsIdentity)context.User.Identity;
        identity.AddClaim(new Claim(ClaimTypes.Role, role));

        await _next(context);
    }
}