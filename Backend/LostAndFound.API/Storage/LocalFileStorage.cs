using System.Text.RegularExpressions;

namespace LostAndFound.Api.Storage;

/// <summary>Stores files on the server's disk under <see cref="RootPath"/>.</summary>
public sealed partial class LocalFileStorage : IFileStorage
{
    public LocalFileStorage(string rootPath)
    {
        RootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(RootPath);
    }

    public string RootPath { get; }

    public async Task<string> SaveAsync(string folder, byte[] content, string extension, CancellationToken cancellationToken)
    {
        if (!SafeSegment().IsMatch(folder) || !SafeExtension().IsMatch(extension))
        {
            throw new ArgumentException("Folder and extension must be simple names.");
        }

        string key = $"{folder}/{Guid.NewGuid():N}{extension}";
        string path = ResolveInsideRoot(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // CreateNew never overwrites, even in the impossible case of a repeated GUID.
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await file.WriteAsync(content, cancellationToken);
        return key;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        File.Delete(ResolveInsideRoot(key));
        return Task.CompletedTask;
    }

    /// <summary>Keys come from the database, but are still checked so a bad value can never reach outside the root.</summary>
    private string ResolveInsideRoot(string key)
    {
        string path = Path.GetFullPath(Path.Combine(RootPath, key));
        string root = RootPath.EndsWith(Path.DirectorySeparatorChar) ? RootPath : RootPath + Path.DirectorySeparatorChar;

        return path.StartsWith(root, StringComparison.Ordinal)
            ? path
            : throw new ArgumentException("The storage key points outside the storage folder.", nameof(key));
    }

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex SafeSegment();

    [GeneratedRegex(@"^\.[a-z0-9]+$")]
    private static partial Regex SafeExtension();
}
