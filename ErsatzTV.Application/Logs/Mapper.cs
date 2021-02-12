using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Logs
{
    internal static class Mapper
    {
        internal static LogEntryViewModel ProjectToViewModel(LogEntry logEntry) =>
            new(
                logEntry.Id,
                logEntry.Timestamp,
                logEntry.Level,
                logEntry.Exception,
                logEntry.RenderedMessage,
                logEntry.Properties);
    }
}
