namespace LostAndFound.Api.ErrorHandling;

/// <summary>
/// An expected failure that maps to a specific HTTP status.
/// Services throw these; <see cref="ApiExceptionHandler"/> turns them into ProblemDetails responses.
/// </summary>
public abstract class ApiException(int statusCode, string title, string detail) : Exception(detail)
{
    public int StatusCode { get; } = statusCode;

    public string Title { get; } = title;
}

public sealed class ConflictException(string detail)
    : ApiException(StatusCodes.Status409Conflict, "Conflict", detail);

public sealed class UnauthorizedException(string detail)
    : ApiException(StatusCodes.Status401Unauthorized, "Unauthorized", detail);

public sealed class ForbiddenException(string detail)
    : ApiException(StatusCodes.Status403Forbidden, "Forbidden", detail);

/// <summary>Also used when the resource exists but belongs to someone else, so its existence is not revealed.</summary>
public sealed class NotFoundException(string detail)
    : ApiException(StatusCodes.Status404NotFound, "Not Found", detail);

/// <summary>Field-level errors, returned in the same shape as automatic model validation.</summary>
public sealed class RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
    : ApiException(StatusCodes.Status400BadRequest, "One or more validation errors occurred.", "One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    public RequestValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = [error] })
    {
    }
}
