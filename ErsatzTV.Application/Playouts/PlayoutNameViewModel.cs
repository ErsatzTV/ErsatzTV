using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Playouts;

public record PlayoutNameViewModel(
    int PlayoutId,
    ProgramSchedulePlayoutType PlayoutType,
    string ChannelName,
    string ChannelNumber,
    string ScheduleName,
    Option<TimeSpan> DailyRebuildTime);
