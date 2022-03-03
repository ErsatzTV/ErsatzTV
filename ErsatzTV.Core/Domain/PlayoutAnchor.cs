namespace ErsatzTV.Core.Domain;

public class PlayoutAnchor
{
    public CollectionEnumeratorState ScheduleItemsEnumeratorState { get; set; }
    public DateTime NextStart { get; set; }
    public int? MultipleRemaining { get; set; }
    public DateTime? DurationFinish { get; set; }
    public bool InFlood { get; set; }
    public bool InDurationFiller { get; set; }
    public int NextGuideGroup { get; set; }

    public DateTimeOffset NextStartOffset => new DateTimeOffset(NextStart, TimeSpan.Zero).ToLocalTime();

    public Option<DateTimeOffset> DurationFinishOffset =>
        Optional(DurationFinish)
            .Map(durationFinish => new DateTimeOffset(durationFinish, TimeSpan.Zero).ToLocalTime());
}