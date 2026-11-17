using Microsoft.OpenApi;

namespace LostAndFound.Api.OpenApi;

public static class SwaggerServiceCollectionExtensions
{
    private const string BearerScheme = "Bearer";

    /// <summary>Adds Swagger with an "Authorize" button that sends the login token on every request.</summary>
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services) =>
        services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Paste the accessToken from /api/auth/login."
                });

                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(BearerScheme, document)] = []
                });
            });
}
