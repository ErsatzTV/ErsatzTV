using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutAllHandler(EnumeratorCache enumeratorCache) : YamlPlayoutContentHandler(enumeratorCache)
{
    public override async Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        PlayoutBuildMode mode,
        Func<string, Task> executeSequence,
        ILogger<YamlPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlPlayoutAllInstruction all)
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
            for (var i = 0; i < enumerator.Count; i++)
            {
                foreach (string preRollSequence in context.GetPreRollSequence())
                {
                    context.PushFillerKind(FillerKind.PreRoll);
                    await executeSequence(preRollSequence);
                    context.PopFillerKind();
                }

                foreach (MediaItem mediaItem in enumerator.Current)
                {
                    TimeSpan itemDuration = DurationForMediaItem(mediaItem);

                    // create a playout item
                    var playoutItem = new PlayoutItem
                    {
                        MediaItem = mediaItem,
                        MediaItemId = mediaItem.Id,
                        Start = context.CurrentTime.UtcDateTime,
                        Finish = context.CurrentTime.UtcDateTime + itemDuration,
                        InPoint = TimeSpan.Zero,
                        OutPoint = itemDuration,
                        FillerKind = GetFillerKind(all, context),
                        CustomTitle = string.IsNullOrWhiteSpace(all.CustomTitle) ? null : all.CustomTitle,
                        DisableWatermarks = all.DisableWatermarks,
                        //PreferredAudioLanguageCode = scheduleItem.PreferredAudioLanguageCode,
                        //PreferredAudioTitle = scheduleItem.PreferredAudioTitle,
                        //PreferredSubtitleLanguageCode = scheduleItem.PreferredSubtitleLanguageCode,
                        //SubtitleMode = scheduleItem.SubtitleMode
                        GuideGroup = context.PeekNextGuideGroup()
                        //GuideStart = effectiveBlock.Start.UtcDateTime,
                        //GuideFinish = blockFinish.UtcDateTime,
                        //BlockKey = JsonConvert.SerializeObject(effectiveBlock.BlockKey),
                        //CollectionKey = JsonConvert.SerializeObject(collectionKey, JsonSettings),
                        //CollectionEtag = collectionEtags[collectionKey]
                    };

                    foreach (int watermarkId in context.GetChannelWatermarkId())
                    {
                        playoutItem.WatermarkId = watermarkId;
                    }

                    await AddItemAndMidRoll(context, playoutItem, executeSequence);
                    context.AdvanceGuideGroup();

                    // create history record
                    List<PlayoutHistory> maybeHistory = GetHistoryForItem(
                        context,
                        instruction.Content,
                        enumerator,
                        playoutItem,
                        mediaItem,
                        logger);

                    foreach (PlayoutHistory history in maybeHistory)
                    {
                        context.Playout.PlayoutHistory.Add(history);
                    }

                    enumerator.MoveNext();
                }

                foreach (string postRollSequence in context.GetPostRollSequence())
                {
                    context.PushFillerKind(FillerKind.PostRoll);
                    await executeSequence(postRollSequence);
                    context.PopFillerKind();
                }
            }

            return true;
        }

        return false;
    }
}
