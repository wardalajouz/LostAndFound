using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LostAndFound.Api.Tests.Infrastructure;

/// <summary>
/// Hooks into the next database save: make it fail, or run code just before it,
/// for example to simulate another request writing the same row at the same moment.
/// </summary>
public sealed class SaveChangesHooks : SaveChangesInterceptor
{
    private int _failNext;
    private int _saveCount;
    private Func<DbContext, Task>? _beforeNext;

    /// <summary>How many times SaveChanges has been called on any test database context.</summary>
    public int SaveCount => Volatile.Read(ref _saveCount);

    public void FailNextSave() => Interlocked.Exchange(ref _failNext, 1);

    public void BeforeNextSave(Func<DbContext, Task> action) => Interlocked.Exchange(ref _beforeNext, action);

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _saveCount);
        if (Interlocked.Exchange(ref _beforeNext, null) is { } action)
        {
            await action(eventData.Context!);
        }

        if (Interlocked.Exchange(ref _failNext, 0) == 1)
        {
            throw new InvalidOperationException("Simulated database failure.");
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
