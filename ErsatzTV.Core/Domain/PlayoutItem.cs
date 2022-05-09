using System.Diagnostics;
using ErsatzTV.Core.Domain.Filler;

namespace ErsatzTV.Core.Domain;

[DebuggerDisplay("{MediaItemId} - {StartOffset} - {FinishOffset}")]
public class PlayoutItem
{
    public int Id { get; set; }
    public int MediaItemId { get; set; }
    public MediaItem MediaItem { get; set; }
    public DateTime Start { get; set; }
    public DateTime Finish { get; set; }
    public DateTime? GuideFinish { get; set; }
    public string CustomTitle { get; set; }
    public int GuideGroup { get; set; }
    public FillerKind FillerKind { get; set; }
    public int PlayoutId { get; set; }
    public Playout Playout { get; set; }
    public TimeSpan InPoint { get; set; }
    public TimeSpan OutPoint { get; set; }
    public string ChapterTitle { get; set; }
    public ChannelWatermark Watermark { get; set; }
    public int? WatermarkId { get; set; }
    public bool DisableWatermarks { get; set; }
    public string PreferredAudioLanguageCode { get; set; }
    public string PreferredSubtitleLanguageCode { get; set; }
    public ChannelSubtitleMode? SubtitleMode { get; set; }
    public DateTimeOffset StartOffset => new DateTimeOffset(Start, TimeSpan.Zero).ToLocalTime();
    public DateTimeOffset FinishOffset => new DateTimeOffset(Finish, TimeSpan.Zero).ToLocalTime();

    public DateTimeOffset? GuideFinishOffset => GuideFinish.HasValue
        ? new DateTimeOffset(GuideFinish.Value, TimeSpan.Zero).ToLocalTime()
        : null;

    public PlayoutItem ForChapter(MediaChapter chapter) =>
        new()
        {
            MediaItemId = MediaItemId,
            MediaItem = MediaItem,
            Start = Start,
            Finish = Start + chapter.EndTime - chapter.StartTime,
            GuideFinish = GuideFinish,
            CustomTitle = CustomTitle,
            GuideGroup = GuideGroup,
            FillerKind = FillerKind,
            PlayoutId = PlayoutId,
            Playout = Playout,
            InPoint = chapter.StartTime,
            OutPoint = chapter.EndTime,
            ChapterTitle = chapter.Title
        };
}
