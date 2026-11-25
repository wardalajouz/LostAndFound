using Microsoft.AspNetCore.Diagnostics;

namespace LostAndFound.Api.ErrorHandling;

/// <summary>
/// The web server reports malformed or oversized requests, such as a body over the size limit, as
/// <see cref="BadHttpRequestException"/>. That is the caller's mistake, so it keeps its own status (for example 413), not 500.
/// </summary>
internal sealed class BadHttpRequestExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException badRequest)
        {
            return false;
        }

        bool tooLarge = badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge;
        httpContext.Response.StatusCode = badRequest.StatusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = badRequest.StatusCode,
                Title = tooLarge ? "Payload Too Large" : "Bad Request",
                Detail = tooLarge ? "The request is too large." : "The request could not be read."
            }
        });
    }
}
