using CliWrap;

namespace ErsatzTV.Application.Streaming;

public record PlayoutItemProcessModel(
    Command Process,
    Option<TimeSpan> MaybeDuration,
    DateTimeOffset Until,
    bool IsComplete);
