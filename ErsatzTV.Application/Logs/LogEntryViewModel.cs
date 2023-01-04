using Serilog.Events;

namespace ErsatzTV.Application.Logs;

public record LogEntryViewModel(
    DateTimeOffset Timestamp,
    LogEventLevel Level,
    string Message);
