using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CalorieTracker.Gateway.RateLimiting;

internal static class GatewayRateLimiting
{
    // Referenced by routes in appsettings.json ("RateLimiterPolicy")
    public const string AuthPolicy = "auth";

    public static IHostApplicationBuilder AddGatewayRateLimiting(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection(RateLimitingOptions.SectionName));

        builder.Services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = OnRejectedAsync;

            limiter.AddPolicy(AuthPolicy, httpContext =>
                FixedWindow($"auth:{ClientAddress(httpContext)}", Settings(httpContext).Auth));

            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                FixedWindow(UserOrClientAddress(httpContext), Settings(httpContext).Global));
        });

        return builder;
    }

    private static RateLimitPartition<string> FixedWindow(string partitionKey, FixedWindowSettings settings) =>
        RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = settings.PermitLimit,
            Window = settings.Window,
            QueueLimit = 0,
        });

    private static RateLimitingOptions Settings(HttpContext httpContext) =>
        httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

    // Behind Cloudflare this has to come from forwarded headers (Phase 5)
    private static string ClientAddress(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string UserOrClientAddress(HttpContext httpContext) =>
        httpContext.User.FindFirst("sub")?.Value is { } userId
            ? $"user:{userId}"
            : $"ip:{ClientAddress(httpContext)}";

    private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        await httpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Gateway.TooManyRequests",
                Detail = "Too many requests. Try again later.",
            },
        });
    }
}
