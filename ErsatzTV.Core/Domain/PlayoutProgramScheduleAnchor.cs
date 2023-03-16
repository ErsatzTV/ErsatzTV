using Destructurama.Attributed;

namespace ErsatzTV.Core.Domain;

public class PlayoutProgramScheduleAnchor
{
    public int Id { get; set; }
    public int PlayoutId { get; set; }

    [NotLogged]
    public Playout Playout { get; set; }

    public DateTime? AnchorDate { get; set; }

    public DateTimeOffset? AnchorDateOffset => AnchorDate.HasValue
        ? new DateTimeOffset(AnchorDate.Value, TimeSpan.Zero).ToLocalTime()
        : null;

    public ProgramScheduleItemCollectionType CollectionType { get; set; }
    public int? CollectionId { get; set; }
    public Collection Collection { get; set; }
    public int? MultiCollectionId { get; set; }
    public MultiCollection MultiCollection { get; set; }
    public int? SmartCollectionId { get; set; }
    public SmartCollection SmartCollection { get; set; }
    public int? MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; }
    public CollectionEnumeratorState EnumeratorState { get; set; }
}
