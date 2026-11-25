using LostAndFound.Api.Auth;
using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Common;
using LostAndFound.Api.Contracts.Users;
using LostAndFound.Api.Data;
using LostAndFound.Api.Data.Entities;
using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.Services.Admin;
using LostAndFound.Api.Services.Posts;
using LostAndFound.Api.Services.Search;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Services;

/// <summary>
/// The admin users table. Deleted accounts are left out: they have no personal data left to manage.
/// A disabled account is cut off immediately, because every request checks the account is active.
/// </summary>
public sealed class AdminUserService(AppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IAdminUserService
{
    private static readonly string AdminRoleName = Roles.Admin.ToUpperInvariant();

    public async Task<PagedResponse<AdminUserResponse>> GetAsync(AdminUsersQuery query, CancellationToken cancellationToken)
    {
        IQueryable<User> users = db.Users.AsNoTracking().Where(u => u.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = LikeSearch.ContainsPattern(query.Search);
            users = users.Where(u =>
                EF.Functions.Like(u.FullName, pattern, LikeSearch.EscapeCharacter)
                || (u.Email != null && EF.Functions.Like(u.Email, pattern, LikeSearch.EscapeCharacter)));
        }

        if (query.UserType is { } userType)
        {
            users = users.Where(u => u.UserType == userType);
        }

        if (query.Status is { } status)
        {
            users = users.Where(u => u.Status == status);
        }

        int totalCount = await users.CountAsync(cancellationToken);
        Guid? adminRoleId = await AdminRoleIdAsync(cancellationToken);

        return await users
            .OrderByDescending(u => u.CreatedAt)
            .ThenByDescending(u => u.Id)
            .Select(u => new AdminUserResponse(
                u.Id,
                u.FullName,
                u.Email!,
                u.UserType,
                db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == adminRoleId) ? UserRole.Admin : UserRole.User,
                u.Status,
                u.Posts.Count(p => p.Visibility != PostVisibility.Deleted),
                u.CreatedAt))
            .ToPagedResponseAsync(totalCount, query, cancellationToken);
    }

    public async Task UpdateStatusAsync(Guid id, UpdateAccountStatusRequest request, CancellationToken cancellationToken)
    {
        string reason = ModerationReason.Require(request.Reason, nameof(UpdateAccountStatusRequest.Reason));
        Guid adminId = currentUser.Id;
        Guid? adminRoleId = await AdminRoleIdAsync(cancellationToken);

        var target = await db.Users
            .Where(u => u.Id == id && u.DeletedAt == null)
            .Select(u => new { User = u, IsAdmin = db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == adminRoleId) })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("User not found.");

        // Admins cannot change each other's status (and the caller is always an admin, so this covers
        // their own account too). No admin can lock the others out.
        if (target.IsAdmin)
        {
            throw new ConflictException("Administrator accounts cannot be disabled or reactivated here.");
        }

        if (target.User.Status == request.Status)
        {
            throw new ConflictException($"The account is already {request.Status.ToString().ToLowerInvariant()}.");
        }

        target.User.Status = request.Status;
        db.UserModerationLogs.Add(new UserModerationLog
        {
            UserId = target.User.Id,
            AdminId = adminId,
            Status = request.Status,
            Reason = reason,
            CreatedAt = timeProvider.GetUtcNow()
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Null until the Admin role has been created, in which case nobody is an admin.</summary>
    private Task<Guid?> AdminRoleIdAsync(CancellationToken cancellationToken) =>
        db.Roles
            .Where(r => r.NormalizedName == AdminRoleName)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
