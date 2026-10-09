using System.Security.Claims;
using CreatorAnalytics.Identity.Contracts.Services;
using CreatorAnalytics.SharedKernel.Tenancy;
using CreatorAnalytics.SharedKernel.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using CreatorAnalytics.Api.Security;

namespace CreatorAnalytics.Api.Middleware;

public class TenantGatekeeperMiddleware
{
    private readonly RequestDelegate _next;

    public TenantGatekeeperMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantAccessService accessService,
        TenantContext tenantContext,
        CurrentUser currentUser)
    {
        var tenantRouteValue = context.GetRouteValue("tenantId")?.ToString();

        // No tenantId in the URL: not a tenant endpoint, let it pass.
        if (string.IsNullOrEmpty(tenantRouteValue))
        {
            await _next(context);
            return;
        }

        // A tenantId that is not a valid GUID can never belong to anyone.
        if (!Guid.TryParse(tenantRouteValue, out var tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (context.User.Identity is null || !context.User.Identity.IsAuthenticated)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var externalUserId = context.User.GetExternalUserId();

        if (string.IsNullOrEmpty(externalUserId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var access = await accessService.GetAccessAsync(tenantId, externalUserId);

        if (access is null)
        {
            // Not a member: 404 hides whether this tenant exists.
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        tenantContext.Set(tenantId);
        currentUser.Set(access.UserId);

        var identity = (ClaimsIdentity)context.User.Identity;
        identity.AddClaim(new Claim(ClaimTypes.Role, access.Role));

        await _next(context);
    }
}