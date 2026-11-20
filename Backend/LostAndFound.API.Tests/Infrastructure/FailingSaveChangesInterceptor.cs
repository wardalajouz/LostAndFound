using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LostAndFound.Api.Tests.Infrastructure;

/// <summary>Makes the next database save fail, to test that partial work is cleaned up.</summary>
public sealed class FailingSaveChangesInterceptor : SaveChangesInterceptor
{
    private int _failNext;

    public void FailNextSave() => Interlocked.Exchange(ref _failNext, 1);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
        Interlocked.Exchange(ref _failNext, 0) == 1
            ? throw new InvalidOperationException("Simulated database failure.")
            : base.SavingChangesAsync(eventData, result, cancellationToken);
}
