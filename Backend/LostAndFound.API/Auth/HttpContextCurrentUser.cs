using LostAndFound.Api.ErrorHandling;

namespace LostAndFound.Api.Auth;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid Id
    {
        get
        {
            string? subject = httpContextAccessor.HttpContext?.User.FindFirst(AuthClaimTypes.UserId)?.Value;
            return Guid.TryParse(subject, out Guid id)
                ? id
                : throw new UnauthorizedException("You must be signed in.");
        }
    }
}
