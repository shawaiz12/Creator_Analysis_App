using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Net;

namespace CreatorAnalytics.Api.Guest;

public sealed class GuestOptions
{
    public int PerIpDailyLimit { get; set; } = 5;
    public int GlobalDailyLimit { get; set; } = 100;
    public int MaxConcurrent { get; set; } = 2;
}

public static class GuestAccessExtensions
{
    public const string PathPrefix = "/api/guest";
    private const long MaxBodyBytes = 48 * 1024;

    private static readonly string[] ClientIpHeaders = { "CF-Connecting-IP", "True-Client-IP" };

    /// <summary>
    /// The visitor's real address. Render's own connection address is a private proxy address, so
    /// Cloudflare's headers are read first. A forged value only affects the per-visitor fairness limit;
    /// the daily limit for everyone still protects the AI quota.
    /// </summary>
    public static string ClientIp(HttpContext context)
    {
        foreach (var name in ClientIpHeaders)
        {
            if (IPAddress.TryParse(context.Request.Headers[name].ToString().Trim(), out var address))
                return address.ToString();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static bool IsGuestRequest(HttpContext context) =>
        context.Request.Path.StartsWithSegments(PathPrefix);

    public static IServiceCollection AddGuestAccess(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Guest").Get<GuestOptions>() ?? new GuestOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // The limiters run in this order and stop at the first refusal. The concurrency check
            // is first because being turned away by it costs the visitor nothing.
            limiter.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    IsGuestRequest(context)
                        ? RateLimitPartition.GetConcurrencyLimiter("guest-concurrency", _ =>
                            new ConcurrencyLimiterOptions
                            {
                                PermitLimit = options.MaxConcurrent,
                                QueueLimit = 0
                            })
                        : RateLimitPartition.GetNoLimiter("other")),

                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    IsGuestRequest(context)
                        ? RateLimitPartition.GetFixedWindowLimiter(
                            ClientIp(context), _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = options.PerIpDailyLimit,
                                Window = TimeSpan.FromDays(1),
                                QueueLimit = 0
                            })
                        : RateLimitPartition.GetNoLimiter("other")),

                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    IsGuestRequest(context)
                        ? RateLimitPartition.GetFixedWindowLimiter("guest-everyone", _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = options.GlobalDailyLimit,
                                Window = TimeSpan.FromDays(1),
                                QueueLimit = 0
                            })
                        : RateLimitPartition.GetNoLimiter("other")));

            limiter.OnRejected = async (context, cancellationToken) =>
            {
                var response = context.HttpContext.Response;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                response.StatusCode = StatusCodes.Status429TooManyRequests;
                await response.WriteAsJsonAsync(
                    new { error = "The demo's free AI limit has been reached for now. Please try again later." },
                    cancellationToken);
            };
        });

        return services;
    }

    /// <summary>Caps the size of guest requests, then applies the rate limits.</summary>
    public static IApplicationBuilder UseGuestAccess(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            if (IsGuestRequest(context))
            {
                var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
                if (feature is { IsReadOnly: false }) feature.MaxRequestBodySize = MaxBodyBytes;
            }

            await next();
        });

        return app.UseRateLimiter();
    }
}