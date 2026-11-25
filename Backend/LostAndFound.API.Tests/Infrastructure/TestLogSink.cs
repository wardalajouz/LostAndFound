using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace LostAndFound.Api.Tests.Infrastructure;

/// <summary>Collects every log entry the API writes, so tests can check what is (and is not) logged.</summary>
public sealed class TestLogSink : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyCollection<LogEntry> Entries => _entries;

    public IEnumerable<LogEntry> WithEventId(int eventId) => _entries.Where(entry => entry.EventId == eventId);

    public ILogger CreateLogger(string categoryName) => new SinkLogger(categoryName, _entries);

    public void Dispose()
    {
    }

    public sealed record LogEntry(string Category, int EventId, LogLevel Level, string Message, Exception? Exception);

    private sealed class SinkLogger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Enqueue(new LogEntry(category, eventId.Id, logLevel, formatter(state, exception), exception));
    }
}
