namespace LostAndFound.Api.Storage;

/// <summary>Stores uploaded files. Keys are generated here, never taken from the request.</summary>
public interface IFileStorage
{
    /// <summary>Saves the content under a new random key in the given folder, for example "posts/3f2c….webp".</summary>
    Task<string> SaveAsync(string folder, byte[] content, string extension, CancellationToken cancellationToken);

    /// <summary>Deleting a key that does not exist is not an error.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
