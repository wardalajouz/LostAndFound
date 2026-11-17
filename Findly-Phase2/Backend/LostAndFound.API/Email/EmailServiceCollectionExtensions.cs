namespace LostAndFound.Api.Email;

public static class EmailServiceCollectionExtensions
{
    /// <summary>
    /// Uses SMTP when Email:Smtp:Host is set. Without it, Development writes emails to files,
    /// and any other environment refuses to start, so production never silently drops emails.
    /// </summary>
    public static IServiceCollection AddEmail(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        bool smtpConfigured = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>()?.Smtp.IsConfigured ?? false;

        if (smtpConfigured)
        {
            return services.AddSingleton<IEmailService, SmtpEmailService>();
        }

        if (environment.IsDevelopment())
        {
            return services.AddSingleton<IEmailService, FileEmailService>();
        }

        throw new InvalidOperationException("Email:Smtp:Host must be configured outside the Development environment.");
    }
}
