namespace LostAndFound.Api.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services) =>
        services
            .AddScoped<IAuthService, AuthService>()
            .AddScoped<ICampusLocationService, CampusLocationService>()
            .AddScoped<IPostService, PostService>()
            .AddScoped<IPostImageService, PostImageService>()
            .AddScoped<IMyPostsService, MyPostsService>()
            .AddScoped<IProfileService, ProfileService>()
            .AddScoped<IConversationService, ConversationService>()
            .AddScoped<INotificationService, NotificationService>()
            .AddScoped<ISupportTicketService, SupportTicketService>()
            .AddScoped<IAdminStatsService, AdminStatsService>()
            .AddScoped<IAdminPostService, AdminPostService>()
            .AddScoped<IAdminUserService, AdminUserService>();
}
