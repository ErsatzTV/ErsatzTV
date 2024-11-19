using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutCountHandler(EnumeratorCache enumeratorCache) : YamlPlayoutContentHandler(enumeratorCache)
{
    public override async Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        PlayoutBuildMode mode,
        ILogger<YamlPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlPlayoutCountInstruction count)
        {
            return false;
        }

        Option<IMediaCollectionEnumerator> maybeEnumerator = await GetContentEnumerator(
            context,
            instruction.Content,
            logger,
            cancellationToken);

        foreach (IMediaCollectionEnumerator enumerator in maybeEnumerator)
        {
            for (var i = 0; i < count.Count; i++)
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
                        CustomTitle = string.IsNullOrWhiteSpace(count.CustomTitle) ? null : count.CustomTitle,
                        //WatermarkId = scheduleItem.WatermarkId,
                        //PreferredAudioLanguageCode = scheduleItem.PreferredAudioLanguageCode,
                        //PreferredAudioTitle = scheduleItem.PreferredAudioTitle,
                        //PreferredSubtitleLanguageCode = scheduleItem.PreferredSubtitleLanguageCode,
                        //SubtitleMode = scheduleItem.SubtitleMode
                        GuideGroup = context.NextGuideGroup()
                        //GuideStart = effectiveBlock.Start.UtcDateTime,
                        //GuideFinish = blockFinish.UtcDateTime,
                        //BlockKey = JsonConvert.SerializeObject(effectiveBlock.BlockKey),
                        //CollectionKey = JsonConvert.SerializeObject(collectionKey, JsonSettings),
                        //CollectionEtag = collectionEtags[collectionKey]
                    };

                    context.Playout.Items.Add(playoutItem);

                    // create history record
                    Option<PlayoutHistory> maybeHistory = GetHistoryForItem(
                        context,
                        instruction.Content,
                        enumerator,
                        playoutItem,
                        mediaItem);

                    foreach (PlayoutHistory history in maybeHistory)
                    {
                        context.Playout.PlayoutHistory.Add(history);
                    }

                    context.CurrentTime += itemDuration;
                    enumerator.MoveNext();
                }
            }

            return true;
        }

        return false;
    }
}
