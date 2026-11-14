using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Data;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        return services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
    }
}
