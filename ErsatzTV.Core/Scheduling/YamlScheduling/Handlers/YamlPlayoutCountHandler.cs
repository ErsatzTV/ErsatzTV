using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;
using NCalc;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutCountHandler(EnumeratorCache enumeratorCache) : YamlPlayoutContentHandler(enumeratorCache)
{
    public override async Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        PlayoutBuildMode mode,
        Func<string, Task> executeSequence,
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
            int seed = context.Playout.Seed + context.InstructionIndex + context.CurrentTime.DayOfYear;
            var random = new Random(seed);
            int enumeratorCount = enumerator is PlaylistEnumerator playlistEnumerator
                ? playlistEnumerator.CountForRandom
                : enumerator.Count;
            var expression = new Expression(count.Count);
            expression.EvaluateParameter += (name, e) =>
            {
                e.Result = name switch
                {
                    "count" => enumeratorCount,
                    "random" => random.Next() % enumeratorCount,
                    _ => e.Result
                };
            };

            object expressionResult = expression.Evaluate();
            int countValue = expressionResult switch
            {
                double doubleResult => (int)Math.Floor(doubleResult),
                int intResult => intResult,
                _ => 0
            };

            for (var i = 0; i < countValue; i++)
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
                        MediaItemId = mediaItem.Id,
                        Start = context.CurrentTime.UtcDateTime,
                        Finish = context.CurrentTime.UtcDateTime + itemDuration,
                        InPoint = TimeSpan.Zero,
                        OutPoint = itemDuration,
                        FillerKind = GetFillerKind(count, context),
                        CustomTitle = string.IsNullOrWhiteSpace(count.CustomTitle) ? null : count.CustomTitle,
                        DisableWatermarks = count.DisableWatermarks,
                        //PreferredAudioLanguageCode = scheduleItem.PreferredAudioLanguageCode,
                        //PreferredAudioTitle = scheduleItem.PreferredAudioTitle,
                        //PreferredSubtitleLanguageCode = scheduleItem.PreferredSubtitleLanguageCode,
                        //SubtitleMode = scheduleItem.SubtitleMode
                        GuideGroup = context.PeekNextGuideGroup(),
                        //GuideStart = effectiveBlock.Start.UtcDateTime,
                        //GuideFinish = blockFinish.UtcDateTime,
                        //BlockKey = JsonConvert.SerializeObject(effectiveBlock.BlockKey),
                        //CollectionKey = JsonConvert.SerializeObject(collectionKey, JsonSettings),
                        //CollectionEtag = collectionEtags[collectionKey],
                        PlayoutItemWatermarks = []
                    };

                    foreach (int watermarkId in context.GetChannelWatermarkId())
                    {
                        playoutItem.PlayoutItemWatermarks.Add(
                            new PlayoutItemWatermark
                            {
                                PlayoutItem = playoutItem,
                                WatermarkId = watermarkId
                            });
                    }

                    await AddItemAndMidRoll(context, playoutItem, mediaItem, executeSequence);
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
