using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;
using System.Globalization;

namespace ErsatzTV.Core;

public class InMemorySink : ILogEventSink
{
    private readonly ConcurrentDictionary<Guid, ConcurrentQueue<string>> _logs = new();

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue(InMemoryLogService.CorrelationIdKey, out var correlationIdValue) &&
            correlationIdValue is ScalarValue { Value: Guid correlationId })
        {
            ConcurrentQueue<string> logQueue = _logs.GetOrAdd(correlationId, _ => new ConcurrentQueue<string>());

            using (var writer = new StringWriter())
            {
                writer.Write($"[{logEvent.Timestamp:HH:mm:ss} {logEvent.Level}] ");
                logEvent.RenderMessage(writer, CultureInfo.CurrentCulture);
                if (logEvent.Exception != null)
                {
                    writer.WriteLine();
                    writer.Write(logEvent.Exception);
                }

                logQueue.Enqueue(writer.ToString());
            }

            while (logQueue.Count > 100)
            {
                logQueue.TryDequeue(out _);
            }
        }
    }

    public IEnumerable<string> GetLogs(Guid correlationId)
    {
        _logs.TryGetValue(correlationId, out ConcurrentQueue<string> logs);
        return logs ?? Enumerable.Empty<string>();
    }

    public void ClearLogs(Guid correlationId)
    {
        _logs.TryRemove(correlationId, out _);
    }
}

public class InMemoryLogService
{
    public InMemorySink Sink { get; } = new();

    public static readonly string CorrelationIdKey = "CorrelationId";
}
