using System.Text;
using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Auth;
using LostAndFound.Api.Contracts.Users;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.Email;
using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Services;

public sealed class AuthService(
    UserManager<User> userManager,
    ITokenService tokenService,
    IEmailService emailService,
    IOptions<FrontendOptions> frontendOptions,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    private const string InvalidCredentials = "Invalid email or password.";
    private const string InvalidResetLink = "This reset link is invalid or has expired.";

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        string email = request.Email.Trim();
        var user = new User
        {
            UserName = email,
            Email = email,
            FullName = request.FullName.Trim(),
            UserType = request.UserType,
            CreatedAt = timeProvider.GetUtcNow()
        };

        IdentityResult result = await userManager.CreateAsync(user, request.Password);
        if (IdentityErrorMapper.HasError(result, nameof(IdentityErrorDescriber.DuplicateEmail))
            || IdentityErrorMapper.HasError(result, nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        if (!result.Succeeded)
        {
            throw IdentityErrorMapper.ToValidationException(result, nameof(RegisterRequest.Password));
        }

        return tokenService.CreateAccessToken(user, roles: []);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        User? user = await FindActiveOrDisabledUserAsync(request.Email);
        if (user is null)
        {
            logger.LoginFailedUnknownAccount();
            throw new UnauthorizedException(InvalidCredentials);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedException("Too many failed login attempts. Try again in a few minutes.");
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            logger.LoginFailed(user.Id);
            if (await userManager.IsLockedOutAsync(user))
            {
                logger.AccountLockedOut(user.Id);
            }

            throw new UnauthorizedException(InvalidCredentials);
        }

        await userManager.ResetAccessFailedCountAsync(user);

        // Checked only after the password, so the response does not reveal disabled accounts to strangers.
        if (user.Status == AccountStatus.Disabled)
        {
            logger.LoginRefusedDisabled(user.Id);
            throw new ForbiddenException("This account has been disabled. Contact support for help.");
        }

        IList<string> roles = await userManager.GetRolesAsync(user);
        return tokenService.CreateAccessToken(user, roles);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        User? user = await FindActiveOrDisabledUserAsync(request.Email);
        if (user is null || user.Status == AccountStatus.Disabled)
        {
            return;
        }

        string token = await userManager.GeneratePasswordResetTokenAsync(user);
        string encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        string resetLink = QueryHelpers.AddQueryString(
            frontendOptions.Value.ResetPasswordUrl,
            new Dictionary<string, string?> { ["email"] = user.Email, ["token"] = encodedToken });

        try
        {
            await emailService.SendAsync(PasswordResetEmail.Create(user.Email!, user.FullName, resetLink), cancellationToken);
            logger.PasswordResetRequested(user.Id);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Swallowed on purpose: a different response for existing accounts would reveal who is registered.
            logger.ResetEmailFailed(exception, user.Id);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        User? user = await FindActiveOrDisabledUserAsync(request.Email);
        string? token = DecodeToken(request.Token);
        if (user is null || token is null)
        {
            throw new RequestValidationException(nameof(ResetPasswordRequest.Token), InvalidResetLink);
        }

        IdentityResult result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (IdentityErrorMapper.HasError(result, nameof(IdentityErrorDescriber.InvalidToken)))
        {
            throw new RequestValidationException(nameof(ResetPasswordRequest.Token), InvalidResetLink);
        }

        if (!result.Succeeded)
        {
            throw IdentityErrorMapper.ToValidationException(result, nameof(ResetPasswordRequest.NewPassword));
        }

        // The user proved they own the email, so lift any lockout from earlier failed logins.
        await userManager.SetLockoutEndDateAsync(user, lockoutEnd: null);
        await userManager.ResetAccessFailedCountAsync(user);
        logger.PasswordResetCompleted(user.Id);
    }

    /// <summary>Deleted accounts are treated as if they do not exist.</summary>
    private async Task<User?> FindActiveOrDisabledUserAsync(string email)
    {
        User? user = await userManager.FindByEmailAsync(email.Trim());
        return user is { DeletedAt: null } ? user : null;
    }

    private static string? DecodeToken(string encodedToken)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
