using LostAndFound.Api.Contracts.Auth;

namespace LostAndFound.Api.Services;

public sealed class AuthService : IAuthService
{
    public Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
