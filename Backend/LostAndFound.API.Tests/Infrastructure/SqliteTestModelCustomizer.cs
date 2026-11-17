using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LostAndFound.Api.Tests.Infrastructure;

/// <summary>
/// SQLite cannot sort or compare DateTimeOffset columns, which SQL Server can.
/// Tests store them as numbers instead, so ORDER BY CreatedAt works the same way.
/// </summary>
public sealed class SqliteTestModelCustomizer(ModelCustomizerDependencies dependencies) : RelationalModelCustomizer(dependencies)
{
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()))
        {
            if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
            {
                property.SetValueConverter(new DateTimeOffsetToBinaryConverter());
            }
        }
    }
}
