using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using LostAndFound.Api.Auth;
using LostAndFound.Api.Logging;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace LostAndFound.Api.Security;

public static class SecurityServiceCollectionExtensions
{
    /// <summary>JSON request bodies above this size are refused. Upload endpoints set their own, larger limits.</summary>
    public const long MaxRequestBodyBytes = 1024 * 1024;

    private const string CorsPolicy = "frontend";

    public static IServiceCollection AddApiSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FrontendCorsOptions>()
            .Bind(configuration.GetSection(FrontendCorsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // The frontend sends the token in the Authorization header, not in cookies, so credentials stay off.
        services.AddCors();
        services.AddOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>()
            .Configure<IOptions<FrontendCorsOptions>>((cors, frontend) => cors.AddPolicy(CorsPolicy, policy => policy
                .WithOrigins(frontend.Value.AllowedOrigins)
                .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete)
                .WithHeaders(HeaderNames.Authorization, HeaderNames.ContentType, HeaderNames.AcceptLanguage)
                .WithExposedHeaders(HeaderNames.Location, HeaderNames.RetryAfter)
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = RejectAsync;
            options.AddPolicy(RateLimitPolicies.Auth, context => PerClientIp(context, limits => limits.Auth));
            options.AddPolicy(RateLimitPolicies.Support, context => PerClientIp(context, limits => limits.Support));
            options.AddPolicy(RateLimitPolicies.Messages, PerUser);
        });

        return services;
    }

    public static WebApplication UseFrontendCors(this WebApplication app)
    {
        app.UseCors(CorsPolicy);
        return app;
    }

    private static RateLimitPartition<string> PerClientIp(HttpContext context, Func<RateLimitOptions, FixedWindowLimit> select) =>
        FixedWindow($"ip:{ClientIp(context)}", select(Limits(context)));

    /// <summary>Signed-in users are limited by account, so many users behind one campus IP do not share a limit.</summary>
    private static RateLimitPartition<string> PerUser(HttpContext context)
    {
        Guid? userId = context.RequestServices.GetRequiredService<ICurrentUser>().OptionalId;
        string key = userId is { } id ? $"user:{id}" : $"ip:{ClientIp(context)}";
        return FixedWindow(key, Limits(context).Messages);
    }

    private static RateLimitPartition<string> FixedWindow(string key, FixedWindowLimit limit) =>
        RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limit.PermitLimit,
            Window = TimeSpan.FromSeconds(limit.WindowSeconds),
            QueueLimit = 0
        });

    private static RateLimitOptions Limits(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;

    private static string ClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress is { } address ? address.ToString() : IPAddress.None.ToString();

    private static async ValueTask RejectAsync(OnRejectedContext rejected, CancellationToken cancellationToken)
    {
        HttpContext context = rejected.HttpContext;
        if (rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            context.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        string policy = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName ?? "unknown";
        context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("LostAndFound.Api.Security.RateLimiting")
            .RateLimited(context.Request.Path, policy);

        await context.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails =
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too Many Requests",
                Detail = "Too many requests. Wait a moment and try again."
            }
        });
    }
}
