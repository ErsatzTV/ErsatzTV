namespace ErsatzTV.Application.Search;

public record SearchTargetViewModel(int Id, string Name, SearchTargetKind Kind);

public record SmartCollectionSearchTargetViewModel(int Id, string Name, string Query)
    : SearchTargetViewModel(Id, Name, SearchTargetKind.SmartCollection);

public enum SearchTargetKind
{
    Channel = 1,
    FFmpegProfile = 2,
    ChannelWatermark = 3,
    Collection = 4,
    MultiCollection = 5,
    SmartCollection = 6,
    Schedule = 7,
    ScheduleItems = 8
}
