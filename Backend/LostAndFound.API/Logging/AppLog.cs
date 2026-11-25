namespace LostAndFound.Api.Logging;

/// <summary>Operational events: things that went wrong in the background, or setup steps.</summary>
internal static partial class AppLog
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Error, Message = "Could not send the password reset email to user {UserId}.")]
    public static partial void ResetEmailFailed(this ILogger logger, Exception exception, Guid userId);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Error, Message = "Could not email support ticket {TicketId} to the support inbox.")]
    public static partial void SupportEmailFailed(this ILogger logger, Exception exception, Guid ticketId);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Warning, Message = "Could not delete stored file {StorageKey}.")]
    public static partial void StoredFileDeleteFailed(this ILogger logger, Exception exception, string storageKey);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Development email written to {Path}.")]
    public static partial void DevelopmentEmailWritten(this ILogger logger, string path);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Created admin account {AdminId}.")]
    public static partial void AdminAccountCreated(this ILogger logger, Guid adminId);
}
