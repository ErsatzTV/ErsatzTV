using System.Diagnostics;
using System.Globalization;
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
    public DateTime? GuideStart { get; set; }
    public DateTime? GuideFinish { get; set; }
    public string CustomTitle { get; set; }
    public int GuideGroup { get; set; }
    public FillerKind FillerKind { get; set; }
    public int PlayoutId { get; set; }
    public Playout Playout { get; set; }
    public TimeSpan InPoint { get; set; }
    public TimeSpan OutPoint { get; set; }
    public string ChapterTitle { get; set; }
    public List<ChannelWatermark> Watermarks { get; set; }
    public bool DisableWatermarks { get; set; }
    public string PreferredAudioLanguageCode { get; set; }
    public string PreferredAudioTitle { get; set; }
    public string PreferredSubtitleLanguageCode { get; set; }
    public ChannelSubtitleMode? SubtitleMode { get; set; }
    public string BlockKey { get; set; }
    public string CollectionKey { get; set; }
    public string CollectionEtag { get; set; }
    public List<PlayoutItemWatermark> PlayoutItemWatermarks { get; set; }
    public List<GraphicsElement> GraphicsElements { get; set; }
    public List<PlayoutItemGraphicsElement> PlayoutItemGraphicsElements { get; set; }
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
            ChapterTitle = chapter.Title,
            Watermarks = Watermarks,
            DisableWatermarks = DisableWatermarks,
            PreferredAudioLanguageCode = PreferredAudioLanguageCode,
            PreferredAudioTitle = PreferredAudioTitle,
            PreferredSubtitleLanguageCode = PreferredSubtitleLanguageCode,
            SubtitleMode = SubtitleMode,
            BlockKey = BlockKey,
            CollectionKey = CollectionKey,
            CollectionEtag = CollectionEtag,
            PlayoutItemWatermarks = PlayoutItemWatermarks.ToList(),
        };

    public string GetDisplayDuration()
    {
        TimeSpan duration = FinishOffset - StartOffset;

        if (duration >= TimeSpan.FromHours(24))
        {
            var ms = string.Format(CultureInfo.InvariantCulture, @"{0:mm\:ss}", duration);
            return $"{(int)duration.TotalHours}:{ms}";
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            duration.TotalHours >= 1 ? @"{0:h\:mm\:ss}" : @"{0:mm\:ss}",
            duration);
    }
}
