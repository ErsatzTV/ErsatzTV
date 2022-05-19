namespace ErsatzTV.Core.Domain;

public record LogEntry(
    int Id,
    DateTime Timestamp,
    string Level,
    string Exception,
    string RenderedMessage,
    string Properties);
