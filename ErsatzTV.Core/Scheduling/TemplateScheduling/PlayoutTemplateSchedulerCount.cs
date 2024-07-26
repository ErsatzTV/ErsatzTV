using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;

namespace ErsatzTV.Core.Scheduling.TemplateScheduling;

public class PlayoutTemplateSchedulerCount : PlayoutTemplateScheduler
{
    public static DateTimeOffset Schedule(
        Playout playout,
        DateTimeOffset currentTime,
        PlayoutTemplateCountItem count,
        IMediaCollectionEnumerator enumerator)
    {
        for (int i = 0; i < count.Count; i++)
        {
            foreach (MediaItem mediaItem in enumerator.Current)
            {
                TimeSpan itemDuration = DurationForMediaItem(mediaItem);

                // create a playout item
                var playoutItem = new PlayoutItem
                {
                    MediaItemId = mediaItem.Id,
                    Start = currentTime.UtcDateTime,
                    Finish = currentTime.UtcDateTime + itemDuration,
                    InPoint = TimeSpan.Zero,
                    OutPoint = itemDuration,
                    FillerKind = FillerKind.None, //blockItem.IncludeInProgramGuide ? FillerKind.None : FillerKind.GuideMode,
                    //CustomTitle = scheduleItem.CustomTitle,
                    //WatermarkId = scheduleItem.WatermarkId,
                    //PreferredAudioLanguageCode = scheduleItem.PreferredAudioLanguageCode,
                    //PreferredAudioTitle = scheduleItem.PreferredAudioTitle,
                    //PreferredSubtitleLanguageCode = scheduleItem.PreferredSubtitleLanguageCode,
                    //SubtitleMode = scheduleItem.SubtitleMode
                    //GuideGroup = effectiveBlock.TemplateItemId,
                    //GuideStart = effectiveBlock.Start.UtcDateTime,
                    //GuideFinish = blockFinish.UtcDateTime,
                    //BlockKey = JsonConvert.SerializeObject(effectiveBlock.BlockKey),
                    //CollectionKey = JsonConvert.SerializeObject(collectionKey, JsonSettings),
                    //CollectionEtag = collectionEtags[collectionKey]
                };

                playout.Items.Add(playoutItem);

                currentTime += itemDuration;
                enumerator.MoveNext();
            }
        }

        return currentTime;
    }
}
