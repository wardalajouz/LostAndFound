using LostAndFound.Api.ErrorHandling;
using Microsoft.AspNetCore.Identity;

namespace LostAndFound.Api.Auth;

/// <summary>Turns ASP.NET Identity errors into field-level validation errors for the API response.</summary>
internal static class IdentityErrorMapper
{
    public static RequestValidationException ToValidationException(IdentityResult result, string passwordField)
    {
        Dictionary<string, string[]> errors = result.Errors
            .GroupBy(error => FieldFor(error.Code, passwordField))
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());

        return new RequestValidationException(errors);
    }

    public static bool HasError(IdentityResult result, string code) =>
        result.Errors.Any(error => error.Code == code);

    private static string FieldFor(string code, string passwordField)
    {
        if (code.StartsWith("Password", StringComparison.Ordinal))
        {
            return passwordField;
        }

        if (code.Contains("Email", StringComparison.Ordinal) || code.Contains("UserName", StringComparison.Ordinal))
        {
            return "Email";
        }

        return "General";
    }
}
