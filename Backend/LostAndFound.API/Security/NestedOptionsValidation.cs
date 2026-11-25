using System.ComponentModel.DataAnnotations;

namespace LostAndFound.Api.Security;

/// <summary>
/// Data-annotation validation of options does not look inside nested objects by default.
/// This checks one nested object and reports its errors with the full setting name, for example "RateLimiting:Auth:PermitLimit".
/// </summary>
internal static class NestedOptionsValidation
{
    public static IEnumerable<ValidationResult> Validate(object nested, string path)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(nested, new ValidationContext(nested), results, validateAllProperties: true);

        return results.Select(result => new ValidationResult(
            $"{path}: {result.ErrorMessage}",
            result.MemberNames.Select(member => $"{path}:{member}").ToArray()));
    }
}
