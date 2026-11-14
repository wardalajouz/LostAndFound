using LostAndFound.Api.Contracts.Admin;
using LostAndFound.Api.Contracts.Notifications;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Contracts.Users;
using LostAndFound.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<CampusLocation> CampusLocations => Set<CampusLocation>();

    public DbSet<Post> Posts => Set<Post>();

    public DbSet<PostImage> PostImages => Set<PostImage>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<PostModerationLog> PostModerationLogs => Set<PostModerationLog>();

    public DbSet<UserModerationLog> UserModerationLogs => Set<UserModerationLog>();

    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <summary>Stores every enum as readable text, so reordering an enum cannot corrupt existing rows.</summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<PostType>().HaveConversion<string>().HaveMaxLength(10);
        configurationBuilder.Properties<ItemCategory>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<PostStatus>().HaveConversion<string>().HaveMaxLength(10);
        configurationBuilder.Properties<PostVisibility>().HaveConversion<string>().HaveMaxLength(10);
        configurationBuilder.Properties<UserType>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<AccountStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<PreferredLanguage>().HaveConversion<string>().HaveMaxLength(10);
        configurationBuilder.Properties<NotificationType>().HaveConversion<string>().HaveMaxLength(30);
    }
}
