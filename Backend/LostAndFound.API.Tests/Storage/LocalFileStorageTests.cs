using LostAndFound.Api.Storage;

namespace LostAndFound.Api.Tests.Storage;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"findly-storage-{Guid.NewGuid():N}");
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests() => _storage = new LocalFileStorage(_root);

    [Fact]
    public void Creates_the_root_folder()
    {
        Assert.True(Directory.Exists(_root));
    }

    [Fact]
    public async Task Save_writes_the_content_under_a_new_random_key()
    {
        byte[] content = [1, 2, 3];

        string first = await _storage.SaveAsync("posts", content, ".webp", CancellationToken.None);
        string second = await _storage.SaveAsync("posts", content, ".webp", CancellationToken.None);

        Assert.Matches("^posts/[0-9a-f]{32}\\.webp$", first);
        Assert.NotEqual(first, second);
        Assert.Equal(content, await File.ReadAllBytesAsync(Path.Combine(_root, first)));
    }

    [Fact]
    public async Task Delete_removes_the_file_and_ignores_missing_ones()
    {
        string key = await _storage.SaveAsync("avatars", [9], ".webp", CancellationToken.None);

        await _storage.DeleteAsync(key, CancellationToken.None);
        await _storage.DeleteAsync(key, CancellationToken.None);

        Assert.False(File.Exists(Path.Combine(_root, key)));
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("posts/../../outside.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("..")]
    public async Task Delete_refuses_keys_that_point_outside_the_folder(string key)
    {
        string outside = Path.GetFullPath(Path.Combine(_root, "..", "outside.txt"));
        await File.WriteAllTextAsync(outside, "keep me");
        try
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _storage.DeleteAsync(key, CancellationToken.None));
            Assert.True(File.Exists(outside));
        }
        finally
        {
            File.Delete(outside);
        }
    }

    [Fact]
    public async Task Delete_refuses_a_sibling_folder_that_shares_the_root_name()
    {
        string sibling = _root + "-sibling";
        Directory.CreateDirectory(sibling);
        string target = Path.Combine(sibling, "file.webp");
        await File.WriteAllTextAsync(target, "keep me");
        try
        {
            string key = $"../{Path.GetFileName(sibling)}/file.webp";
            await Assert.ThrowsAsync<ArgumentException>(() => _storage.DeleteAsync(key, CancellationToken.None));
            Assert.True(File.Exists(target));
        }
        finally
        {
            Directory.Delete(sibling, recursive: true);
        }
    }

    [Theory]
    [InlineData("../posts", ".webp")]
    [InlineData("Posts", ".webp")]
    [InlineData("posts/sub", ".webp")]
    [InlineData("", ".webp")]
    [InlineData("posts", "webp")]
    [InlineData("posts", "./webp")]
    [InlineData("posts", ".we/bp")]
    public async Task Save_refuses_unsafe_folder_or_extension(string folder, string extension)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _storage.SaveAsync(folder, [1], extension, CancellationToken.None));

        Assert.Empty(Directory.GetFiles(_root, "*", SearchOption.AllDirectories));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
