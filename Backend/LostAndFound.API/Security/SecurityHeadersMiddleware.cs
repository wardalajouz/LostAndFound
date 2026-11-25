using Microsoft.Net.Http.Headers;

namespace LostAndFound.Api.Security;

/// <summary>
/// Adds protective headers to every response. The API only returns JSON and images, so the
/// content security policy allows nothing to run; Swagger, which is a web page, is left out of it.
/// </summary>
/// <remarks>
/// The headers are added just before the response is sent, not at the start of the request:
/// the exception handler clears the response before writing an error, which would remove them.
/// </remarks>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public const string ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

    private static readonly PathString SwaggerPath = new("/swagger");

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(AddHeaders, context);
        return next(context);
    }

    private static Task AddHeaders(object state)
    {
        var context = (HttpContext)state;
        IHeaderDictionary headers = context.Response.Headers;
        headers[HeaderNames.XContentTypeOptions] = "nosniff";
        headers[HeaderNames.XFrameOptions] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";

        if (!context.Request.Path.StartsWithSegments(SwaggerPath))
        {
            headers[HeaderNames.ContentSecurityPolicy] = ContentSecurityPolicy;
        }

        return Task.CompletedTask;
    }
}
