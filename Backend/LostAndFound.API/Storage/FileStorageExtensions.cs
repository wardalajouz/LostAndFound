using LostAndFound.Api.Logging;

namespace LostAndFound.Api.Storage;

public static class FileStorageExtensions
{
    /// <summary>
    /// Best-effort cleanup. A file left behind only wastes disk space, so a failure is logged
    /// instead of failing a request whose database change already succeeded.
    /// </summary>
    public static async Task DeleteQuietlyAsync(this IFileStorage storage, IEnumerable<string> keys, ILogger logger)
    {
        foreach (string key in keys)
        {
            try
            {
                await storage.DeleteAsync(key, CancellationToken.None);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                logger.StoredFileDeleteFailed(exception, key);
            }
        }
    }
}
