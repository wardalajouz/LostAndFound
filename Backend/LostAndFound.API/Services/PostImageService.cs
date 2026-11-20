using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Data;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.Images;
using LostAndFound.Api.Services.Posts;
using LostAndFound.Api.Storage;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Services;

public sealed class PostImageService(
    AppDbContext db,
    ICurrentUser currentUser,
    IImageProcessor imageProcessor,
    IFileStorage storage,
    TimeProvider timeProvider,
    ILogger<PostImageService> logger) : IPostImageService
{
    private const string StorageFolder = "posts";

    public async Task<IReadOnlyList<PostImageResponse>> UploadAsync(
        Guid postId, UploadPostImagesRequest request, CancellationToken cancellationToken)
    {
        Post post = await FindEditableOwnPostAsync(postId, cancellationToken);

        int existingCount = await db.PostImages.CountAsync(image => image.PostId == post.Id, cancellationToken);
        if (existingCount + request.Images.Count > ImageLimits.MaxImagesPerPost)
        {
            throw new RequestValidationException(
                nameof(UploadPostImagesRequest.Images),
                $"A post can have at most {ImageLimits.MaxImagesPerPost} images. This post already has {existingCount}.");
        }

        IReadOnlyList<ProcessedImage> processed = await imageProcessor.ProcessAllAsync(
            request.Images, nameof(UploadPostImagesRequest.Images), cancellationToken);

        int nextSortOrder = (await db.PostImages
            .Where(image => image.PostId == post.Id)
            .MaxAsync(image => (int?)image.SortOrder, cancellationToken) ?? -1) + 1;

        var savedKeys = new List<string>(processed.Count);
        var images = new List<PostImage>(processed.Count);
        try
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            foreach (ProcessedImage image in processed)
            {
                string key = await storage.SaveAsync(StorageFolder, image.Content, image.Extension, cancellationToken);
                savedKeys.Add(key);
                images.Add(new PostImage { PostId = post.Id, StorageKey = key, SortOrder = nextSortOrder++, CreatedAt = now });
            }

            db.PostImages.AddRange(images);
            post.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Nothing references these files yet, so remove them before reporting the failure.
            await storage.DeleteQuietlyAsync(savedKeys, logger);
            throw;
        }

        return images.Select(image => new PostImageResponse(image.Id, ImageUrls.For(image.StorageKey))).ToList();
    }

    public async Task DeleteAsync(Guid postId, Guid imageId, CancellationToken cancellationToken)
    {
        Post post = await FindEditableOwnPostAsync(postId, cancellationToken);

        // The image must belong to this post, which was checked to belong to the caller.
        PostImage image = await db.PostImages.FirstOrDefaultAsync(
                i => i.Id == imageId && i.PostId == post.Id, cancellationToken)
            ?? throw new NotFoundException("Image not found.");

        db.PostImages.Remove(image);
        post.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);

        await storage.DeleteQuietlyAsync([image.StorageKey], logger);
    }

    /// <summary>Same ownership rule as editing the post: someone else's or a deleted post is a 404, a resolved one a 409.</summary>
    private async Task<Post> FindEditableOwnPostAsync(Guid postId, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.Id;
        Post post = await db.Posts.FirstOrDefaultAsync(
                p => p.Id == postId && p.OwnerId == userId && p.Visibility != PostVisibility.Deleted,
                cancellationToken)
            ?? throw new NotFoundException("Post not found.");

        return post.Status == PostStatus.Resolved
            ? throw new ConflictException("Images of a resolved post cannot be changed.")
            : post;
    }
}
