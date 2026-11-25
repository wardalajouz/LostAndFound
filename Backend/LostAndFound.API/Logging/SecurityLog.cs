using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Users;

namespace LostAndFound.Api.Logging;

/// <summary>
/// Security events, for spotting attacks and answering "who did this". Accounts are identified by id only:
/// no email addresses, names, passwords, or tokens are ever logged.
/// </summary>
internal static partial class SecurityLog
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "Login failed: wrong password for user {UserId}.")]
    public static partial void LoginFailed(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Login failed: no active account with that email.")]
    public static partial void LoginFailedUnknownAccount(this ILogger logger);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "Account {UserId} locked after repeated wrong passwords.")]
    public static partial void AccountLockedOut(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Warning, Message = "Login refused: account {UserId} is disabled.")]
    public static partial void LoginRefusedDisabled(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1010, Level = LogLevel.Information, Message = "Password reset link sent to user {UserId}.")]
    public static partial void PasswordResetRequested(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1011, Level = LogLevel.Information, Message = "Password reset completed for user {UserId}.")]
    public static partial void PasswordResetCompleted(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1012, Level = LogLevel.Information, Message = "Password changed by user {UserId}.")]
    public static partial void PasswordChanged(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1013, Level = LogLevel.Information, Message = "Email address changed by user {UserId}.")]
    public static partial void EmailChanged(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1014, Level = LogLevel.Information, Message = "Account {UserId} deleted by its owner.")]
    public static partial void AccountDeleted(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 1020, Level = LogLevel.Information, Message = "Admin {AdminId} set post {PostId} to {Visibility}.")]
    public static partial void PostModerated(this ILogger logger, Guid adminId, Guid postId, PostVisibility visibility);

    [LoggerMessage(EventId = 1021, Level = LogLevel.Information, Message = "Admin {AdminId} set account {UserId} to {Status}.")]
    public static partial void AccountStatusChanged(this ILogger logger, Guid adminId, Guid userId, AccountStatus status);

    [LoggerMessage(EventId = 1030, Level = LogLevel.Warning, Message = "Request to {Path} rejected by the {Policy} rate limit.")]
    public static partial void RateLimited(this ILogger logger, string path, string policy);
}
