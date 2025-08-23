using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Playouts;

public record PlayoutNameViewModel(
    int PlayoutId,
    PlayoutScheduleKind ScheduleKind,
    string ChannelName,
    string ChannelNumber,
    ChannelPlayoutMode PlayoutMode,
    string ScheduleName,
    string TemplateFile,
    string ExternalJsonFile,
    TimeSpan? DbDailyRebuildTime)
{
    public Option<TimeSpan> DailyRebuildTime => Optional(DbDailyRebuildTime);

    public string TemplateFile { get; set; } = TemplateFile;
}
