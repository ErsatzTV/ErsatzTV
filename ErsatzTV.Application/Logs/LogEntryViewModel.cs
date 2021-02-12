using System;

namespace ErsatzTV.Application.Logs
{
    public record LogEntryViewModel(
        int Id,
        DateTime Timestamp,
        string Level,
        string Exception,
        string RenderedMessage,
        string Properties);
}
