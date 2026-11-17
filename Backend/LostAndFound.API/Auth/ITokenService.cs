using LostAndFound.Api.Contracts.Auth;
using LostAndFound.Api.Data.Entities;

namespace LostAndFound.Api.Auth;

public interface ITokenService
{
    AuthResponse CreateAccessToken(User user, IEnumerable<string> roles);
}
