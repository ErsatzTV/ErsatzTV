using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;

namespace ErsatzTV.Core.Scheduling.YamlScheduling;

public class YamlPlayoutSchedulerCount : YamlPlayoutScheduler
{
    public static DateTimeOffset Schedule(
        YamlPlayoutContext context,
        YamlPlayoutCountInstruction count,
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
                    Start = context.CurrentTime.UtcDateTime,
                    Finish = context.CurrentTime.UtcDateTime + itemDuration,
                    InPoint = TimeSpan.Zero,
                    OutPoint = itemDuration,
                    FillerKind = GetFillerKind(count),
                    //CustomTitle = scheduleItem.CustomTitle,
                    //WatermarkId = scheduleItem.WatermarkId,
                    //PreferredAudioLanguageCode = scheduleItem.PreferredAudioLanguageCode,
                    //PreferredAudioTitle = scheduleItem.PreferredAudioTitle,
                    //PreferredSubtitleLanguageCode = scheduleItem.PreferredSubtitleLanguageCode,
                    //SubtitleMode = scheduleItem.SubtitleMode
                    GuideGroup = context.GuideGroup
                    //GuideStart = effectiveBlock.Start.UtcDateTime,
                    //GuideFinish = blockFinish.UtcDateTime,
                    //BlockKey = JsonConvert.SerializeObject(effectiveBlock.BlockKey),
                    //CollectionKey = JsonConvert.SerializeObject(collectionKey, JsonSettings),
                    //CollectionEtag = collectionEtags[collectionKey]
                };

                context.Playout.Items.Add(playoutItem);

                context.CurrentTime += itemDuration;
                enumerator.MoveNext();
            }
        }

        return context.CurrentTime;
    }
}
