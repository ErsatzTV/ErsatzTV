using Serilog.Events;

namespace ErsatzTV.Application.Logs;

public record LogEntryViewModel(
    int Id,
    DateTime Timestamp,
    LogEventLevel Level,
    string Exception,
    string Message);