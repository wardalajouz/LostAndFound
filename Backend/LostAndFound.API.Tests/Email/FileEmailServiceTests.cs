using LostAndFound.Api.Email;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace LostAndFound.Api.Tests.Email;

public sealed class FileEmailServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"findly-email-{Guid.NewGuid():N}");

    [Fact]
    public async Task Writes_the_email_to_App_Data()
    {
        var service = new FileEmailService(
            new TestEnvironment(_root),
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero)),
            NullLogger<FileEmailService>.Instance);

        EmailMessage message = PasswordResetEmail.Create("ward@findly.test", "Ward", "https://findly.test/reset?token=abc");
        await service.SendAsync(message, CancellationToken.None);

        string file = Assert.Single(Directory.GetFiles(Path.Combine(_root, "App_Data", "emails")));
        string content = await File.ReadAllTextAsync(file);
        Assert.Contains("To: Ward <ward@findly.test>", content);
        Assert.Contains("https://findly.test/reset?token=abc", content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class TestEnvironment(string contentRoot) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = contentRoot;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Findly";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = contentRoot;
        public string EnvironmentName { get; set; } = "Development";
    }
}
