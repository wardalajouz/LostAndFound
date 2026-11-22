using LostAndFound.Api.Auth;
using LostAndFound.Api.Localization;
using LostAndFound.Api.Notifications;
using LostAndFound.Api.Services.Profile;
using LostAndFound.Api.Support;

namespace LostAndFound.Api.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SupportOptions>()
            .Bind(configuration.GetSection(SupportOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services
            .AddHttpContextAccessor()
            .AddScoped<ICurrentUser, HttpContextCurrentUser>()
            .AddScoped<IRequestLanguage, AcceptLanguageRequestLanguage>()
            .AddScoped<INotifier, DatabaseNotifier>()
            .AddScoped<IAuthService, AuthService>()
            .AddScoped<ICampusLocationService, CampusLocationService>()
            .AddScoped<IPostService, PostService>()
            .AddScoped<IPostImageService, PostImageService>()
            .AddScoped<IMyPostsService, MyPostsService>()
            .AddScoped<IProfileService, ProfileService>()
            .AddScoped<PasswordConfirmation>()
            .AddScoped<AccountDeletion>()
            .AddScoped<IConversationService, ConversationService>()
            .AddScoped<INotificationService, NotificationService>()
            .AddScoped<ISupportTicketService, SupportTicketService>()
            .AddScoped<IAdminStatsService, AdminStatsService>()
            .AddScoped<IAdminPostService, AdminPostService>()
            .AddScoped<IAdminUserService, AdminUserService>();
    }
}
