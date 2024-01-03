using ErsatzTV.Core.Domain.Filler;
using Newtonsoft.Json;

namespace ErsatzTV.Core.Domain;

public abstract class ProgramScheduleItem
{
    public int Id { get; set; }
    public int Index { get; set; }
    public StartType StartType => StartTime.HasValue ? StartType.Fixed : StartType.Dynamic;
    public TimeSpan? StartTime { get; set; }
    public ProgramScheduleItemCollectionType CollectionType { get; set; }
    public GuideMode GuideMode { get; set; }
    public string CustomTitle { get; set; }
    public int ProgramScheduleId { get; set; }

    [JsonIgnore]
    public ProgramSchedule ProgramSchedule { get; set; }

    public int? CollectionId { get; set; }
    public Collection Collection { get; set; }
    public int? MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; }
    public int? MultiCollectionId { get; set; }
    public MultiCollection MultiCollection { get; set; }
    public int? SmartCollectionId { get; set; }
    public SmartCollection SmartCollection { get; set; }
    public PlaybackOrder PlaybackOrder { get; set; }
    public int? PreRollFillerId { get; set; }
    public FillerPreset PreRollFiller { get; set; }
    public int? MidRollFillerId { get; set; }
    public FillerPreset MidRollFiller { get; set; }
    public int? PostRollFillerId { get; set; }
    public FillerPreset PostRollFiller { get; set; }
    public int? TailFillerId { get; set; }
    public FillerPreset TailFiller { get; set; }
    public int? FallbackFillerId { get; set; }
    public FillerPreset FallbackFiller { get; set; }
    public ChannelWatermark Watermark { get; set; }
    public int? WatermarkId { get; set; }
    public string PreferredAudioLanguageCode { get; set; }
    public string PreferredAudioTitle { get; set; }
    public string PreferredSubtitleLanguageCode { get; set; }
    public ChannelSubtitleMode? SubtitleMode { get; set; }
    public FillWithGroupMode FillWithGroupMode { get; set; }
}
