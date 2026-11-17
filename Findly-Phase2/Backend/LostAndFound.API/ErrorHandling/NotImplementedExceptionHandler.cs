using Microsoft.AspNetCore.Diagnostics;

namespace LostAndFound.Api.ErrorHandling;

internal sealed class NotImplementedExceptionHandler(IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not NotImplementedException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status501NotImplemented;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = StatusCodes.Status501NotImplemented,
                Title = "Not Implemented",
                Detail = "This feature is not implemented yet."
            }
        });
    }
}
