using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LostAndFound.Api.Tests.Infrastructure;

/// <summary>
/// SQLite cannot sort or compare DateTimeOffset columns, which SQL Server can. Tests store them
/// as UTC ticks instead: exact to 100 ns like SQL Server's datetimeoffset, and sortable.
/// The API only writes UTC times, so the offset is always zero.
/// </summary>
public sealed class SqliteTestModelCustomizer(ModelCustomizerDependencies dependencies) : RelationalModelCustomizer(dependencies)
{
    private static readonly ValueConverter<DateTimeOffset, long> UtcTicks =
        new(value => value.UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()))
        {
            if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
            {
                property.SetValueConverter(UtcTicks);
            }
        }
    }
}
