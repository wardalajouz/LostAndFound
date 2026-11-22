using LostAndFound.Api.ErrorHandling;

namespace LostAndFound.Api.Auth;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid Id => OptionalId ?? throw new UnauthorizedException("You must be signed in.");

    public Guid? OptionalId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return Guid.TryParse(user.FindFirst(AuthClaimTypes.UserId)?.Value, out Guid id) ? id : null;
        }
    }
}
