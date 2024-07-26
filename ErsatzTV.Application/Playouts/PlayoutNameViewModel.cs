using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Playouts;

public record PlayoutNameViewModel(
    int PlayoutId,
    ProgramSchedulePlayoutType PlayoutType,
    string ChannelName,
    string ChannelNumber,
    ChannelProgressMode ProgressMode,
    string ScheduleName,
    string TemplateFile,
    string ExternalJsonFile,
    TimeSpan? DbDailyRebuildTime)
{
    public Option<TimeSpan> DailyRebuildTime => Optional(DbDailyRebuildTime);
}
