using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Data;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.Services.Posts;
using LostAndFound.Api.Storage;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Services.Profile;

/// <summary>
/// Deletes an account by removing its personal data while keeping the row, so the other person's
/// chat history and admin logs stay intact. Everything happens in one transaction.
/// </summary>
public sealed class AccountDeletion(AppDbContext db, IFileStorage storage, TimeProvider timeProvider, ILogger<AccountDeletion> logger)
{
    public const string DeletedUserName = "Deleted user";

    public async Task DeleteAsync(User user, CancellationToken cancellationToken)
    {
        string? avatarKey = ImageUrls.KeyFrom(user.AvatarUrl);
        Guid userId = user.Id;

        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            await db.Posts
                .Where(p => p.OwnerId == userId && p.Visibility != PostVisibility.Deleted)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Visibility, PostVisibility.Deleted), cancellationToken);

            await db.Notifications.Where(n => n.UserId == userId).ExecuteDeleteAsync(cancellationToken);

            await db.SupportTickets
                .Where(t => t.UserId == userId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.FullName, DeletedUserName)
                    .SetProperty(t => t.Email, string.Empty), cancellationToken);

            // Sign-in data: roles, external logins, claims, and stored tokens.
            await db.UserRoles.Where(r => r.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await db.UserLogins.Where(l => l.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await db.UserClaims.Where(c => c.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await db.UserTokens.Where(t => t.UserId == userId).ExecuteDeleteAsync(cancellationToken);

            Anonymize(user);
            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }

        if (avatarKey is not null)
        {
            await storage.DeleteQuietlyAsync([avatarKey], logger);
        }
    }

    /// <summary>Keeps only what chats and logs need: the id and the fact that the account existed.</summary>
    private void Anonymize(User user)
    {
        string placeholder = $"deleted-{user.Id:N}";

        user.FullName = DeletedUserName;
        user.Email = null;
        user.NormalizedEmail = null;
        user.UserName = placeholder;
        user.NormalizedUserName = placeholder.ToUpperInvariant();
        user.PasswordHash = null;
        user.PhoneNumber = null;
        user.AvatarUrl = null;
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.DeletedAt = timeProvider.GetUtcNow();
    }
}
