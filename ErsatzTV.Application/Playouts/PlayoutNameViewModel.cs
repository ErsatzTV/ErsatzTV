using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Playouts;

public record PlayoutNameViewModel(
    int PlayoutId,
    PlayoutScheduleKind ScheduleKind,
    string ChannelName,
    string ChannelNumber,
    ChannelPlayoutMode PlayoutMode,
    string ScheduleName,
    string ScheduleFile,
    TimeSpan? DbDailyRebuildTime,
    PlayoutBuildStatus BuildStatus)
{
    public Option<TimeSpan> DailyRebuildTime => Optional(DbDailyRebuildTime);

    public string ScheduleFile { get; set; } = ScheduleFile;
}
