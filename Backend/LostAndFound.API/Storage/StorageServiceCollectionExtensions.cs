using LostAndFound.Api.Images;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace LostAndFound.Api.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(provider =>
        {
            string rootPath = provider.GetRequiredService<IOptions<StorageOptions>>().Value.RootPath;
            string contentRoot = provider.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
            return new LocalFileStorage(Path.Combine(contentRoot, rootPath));
        });
        services.AddSingleton<IFileStorage>(provider => provider.GetRequiredService<LocalFileStorage>());
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();

        return services;
    }

    /// <summary>
    /// Serves stored images publicly at <see cref="Services.Posts.ImageUrls.PathPrefix"/>.
    /// Only .webp files are served, the only type the API writes, so nothing else in the folder is reachable.
    /// Protective headers such as nosniff come from <see cref="Security.SecurityHeadersMiddleware"/>.
    /// </summary>
    public static WebApplication UseStoredFiles(this WebApplication app)
    {
        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings.Clear();
        contentTypes.Mappings[".webp"] = "image/webp";
        string requestPath = Services.Posts.ImageUrls.PathPrefix.TrimEnd('/');

        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = requestPath,
            FileProvider = new PhysicalFileProvider(app.Services.GetRequiredService<LocalFileStorage>().RootPath),
            ContentTypeProvider = contentTypes,
            ServeUnknownFileTypes = false,
            OnPrepareResponse = context =>
            {
                // File names are random and never reused, so browsers can cache them for good.
                context.Context.Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
            }
        });

        // Anything under /uploads that was not served above does not exist. Answer 404 here,
        // so the request never reaches the API's sign-in check and gets a misleading 401.
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(requestPath))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next(context);
        });

        return app;
    }
}
