using System.Security.Claims;

namespace CreatorAnalytics.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static string? GetExternalUserId(this ClaimsPrincipal user) =>
        user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
        ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? user.FindFirst("sub")?.Value;

    public static string? GetEmail(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Email)?.Value
        ?? user.FindFirst("email")?.Value
        ?? user.FindFirst("preferred_username")?.Value
        ?? user.FindFirst("upn")?.Value;
}