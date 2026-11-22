using LostAndFound.Api.Data;
using LostAndFound.Api.Data.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LostAndFound.Api.Auth;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<FrontendOptions>()
            .Bind(configuration.GetSection(FrontendOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.Configure<AdminSeedOptions>(configuration.GetSection(AdminSeedOptions.SectionName));

        services
            .AddIdentityCore<User>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(1));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Configured from validated options, so the key is read only after it has been checked.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                JwtOptions jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                bearer.Events = new JwtBearerEvents { OnTokenValidated = ActiveAccountCheck.OnTokenValidatedAsync };
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtTokenService.CreateSigningKey(jwt.SigningKey),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = AuthClaimTypes.Name,
                    RoleClaimType = AuthClaimTypes.Role
                };
            });

        // Secure by default: every endpoint needs a signed-in user unless it is marked [AllowAnonymous].
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<AdminSeeder>();

        return services;
    }

    public static async Task SeedAdminAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AdminSeeder>().SeedAsync();
    }
}
