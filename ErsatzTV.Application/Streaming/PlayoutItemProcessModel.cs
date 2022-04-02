using System.Diagnostics;

namespace ErsatzTV.Application.Streaming;

public record PlayoutItemProcessModel(Process Process, Option<TimeSpan> MaybeDuration, DateTimeOffset Until);