using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ErsatzTV.Core.Scheduling.BlockScheduling;

public class BlockPlayoutFillerBuilder(
    IMediaCollectionRepository mediaCollectionRepository,
    ITelevisionRepository televisionRepository,
    IArtistRepository artistRepository,
    ILogger<BlockPlayoutFillerBuilder> logger) : IBlockPlayoutFillerBuilder
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    public async Task<PlayoutBuildResult> Build(
        Playout playout,
        PlayoutReferenceData referenceData,
        PlayoutBuildResult result,
        PlayoutBuildMode mode,
        CancellationToken cancellationToken)
    {
        var allItems = result.AddedItems.ToList();

        if (mode is PlayoutBuildMode.Reset)
        {
            // remove all playout items with type filler
            // except block items that are hidden from the guide (guide mode)
            foreach (var item in referenceData.ExistingItems)
            {
                if (item.FillerKind is FillerKind.None or FillerKind.GuideMode)
                {
                    allItems.Add(item);
                    continue;
                }

                BlockPlayoutChangeDetection.RemoveItemAndHistory(referenceData, item, result);
            }
        }
        else
        {
            allItems.AddRange(referenceData.ExistingItems);
        }

        var collectionEnumerators = new Dictionary<CollectionKey, IMediaCollectionEnumerator>();

        // find all unscheduled periods
        var queue = new Queue<PlayoutItem>(allItems);
        while (queue.Count > 1)
        {
            PlayoutItem one = queue.Dequeue();
            PlayoutItem two = queue.Peek();

            DateTimeOffset start = one.FinishOffset;
            DateTimeOffset finish = two.StartOffset;

            if (start == finish)
            {
                continue;
            }

            // find applicable deco
            foreach (Deco deco in GetDecoFor(referenceData, start))
            {
                if (!HasDefaultFiller(deco))
                {
                    continue;
                }

                var collectionKey = CollectionKey.ForDecoDefaultFiller(deco);
                string historyKey = HistoryDetails.ForDefaultFiller(deco);

                // load collection items from db on demand
                if (!collectionEnumerators.TryGetValue(collectionKey, out IMediaCollectionEnumerator enumerator))
                {
                    List<MediaItem> collectionItems = await MediaItemsForCollection.Collect(
                        mediaCollectionRepository,
                        televisionRepository,
                        artistRepository,
                        collectionKey);

                    enumerator = BlockPlayoutEnumerator.Shuffle(
                        collectionItems,
                        start,
                        playout.Seed,
                        referenceData.PlayoutHistory.Append(result.AddedHistory).ToList(),
                        deco,
                        historyKey);

                    if (enumerator.Count == 0)
                    {
                        logger.LogWarning(
                            "Block filler contains empty collection {@Key}; no filler will be scheduled",
                            collectionKey);
                    }

                    collectionEnumerators.Add(collectionKey, enumerator);
                }

                // skip this deco if the collection has no items
                if (enumerator.Count == 0)
                {
                    continue;
                }

                DateTimeOffset current = start;
                var pastTime = false;
                while (current < finish)
                {
                    foreach (MediaItem mediaItem in enumerator.Current)
                    {
                        TimeSpan itemDuration = DurationForMediaItem(mediaItem);

                        // add filler from deco to unscheduled period
                        var filler = new PlayoutItem
                        {
                            PlayoutId = playout.Id,
                            MediaItemId = mediaItem.Id,
                            Start = current.UtcDateTime,
                            Finish = current.UtcDateTime + itemDuration,
                            InPoint = TimeSpan.Zero,
                            OutPoint = itemDuration,

                            // FillerKind.Fallback will loop and avoid hw accel, so don't use that
                            FillerKind = FillerKind.DecoDefault,

                            CollectionKey = JsonConvert.SerializeObject(collectionKey, JsonSettings),
                            GuideStart = one.GuideStart,
                            GuideFinish = one.GuideFinish,
                            GuideGroup = one.GuideGroup
                        };

                        if (filler.FinishOffset > finish)
                        {
                            if (deco.DefaultFillerTrimToFit)
                            {
                                filler.Finish = finish.UtcDateTime;
                                filler.OutPoint = filler.Finish - filler.Start;
                            }
                            else
                            {
                                pastTime = true;
                                break;
                            }
                        }

                        result.AddedItems.Add(filler);

                        // create a playout history record
                        var nextHistory = new PlayoutHistory
                        {
                            PlayoutId = playout.Id,
                            PlaybackOrder = PlaybackOrder.Shuffle,
                            Index = enumerator.State.Index,
                            When = current.UtcDateTime,
                            Key = historyKey,
                            Details = HistoryDetails.ForMediaItem(mediaItem)
                        };

                        result.AddedHistory.Add(nextHistory);

                        current += itemDuration;
                        enumerator.MoveNext();
                    }

                    if (pastTime)
                    {
                        break;
                    }
                }
            }
        }


        return result;
    }

    private static Option<Deco> GetDecoFor(PlayoutReferenceData referenceData, DateTimeOffset start)
    {
        Option<PlayoutTemplate> maybeTemplate =
            PlayoutTemplateSelector.GetPlayoutTemplateFor(referenceData.PlayoutTemplates, start);
        foreach (PlayoutTemplate template in maybeTemplate)
        {
            if (template.DecoTemplate is not null)
            {
                foreach (DecoTemplateItem decoTemplateItem in template.DecoTemplate.Items)
                {
                    if (decoTemplateItem.StartTime <= start.TimeOfDay && decoTemplateItem.EndTime == TimeSpan.Zero ||
                        decoTemplateItem.EndTime > start.TimeOfDay)
                    {
                        switch (decoTemplateItem.Deco.DefaultFillerMode)
                        {
                            case DecoMode.Inherit:
                                return referenceData.Deco;
                            case DecoMode.Override:
                                return decoTemplateItem.Deco;
                            case DecoMode.Disable:
                            default:
                                return Option<Deco>.None;
                        }
                    }
                }
            }
        }

        return referenceData.Deco;
    }

    private static bool HasDefaultFiller(Deco deco)
    {
        switch (deco.DefaultFillerCollectionType)
        {
            case ProgramScheduleItemCollectionType.Collection:
                return deco.DefaultFillerCollectionId.HasValue;
            case ProgramScheduleItemCollectionType.TelevisionShow:
                return deco.DefaultFillerMediaItemId.HasValue;
            case ProgramScheduleItemCollectionType.TelevisionSeason:
                return deco.DefaultFillerMediaItemId.HasValue;
            case ProgramScheduleItemCollectionType.Artist:
                return deco.DefaultFillerMediaItemId.HasValue;
            case ProgramScheduleItemCollectionType.MultiCollection:
                return deco.DefaultFillerMultiCollectionId.HasValue;
            case ProgramScheduleItemCollectionType.SmartCollection:
                return deco.DefaultFillerSmartCollectionId.HasValue;
            default:
                return false;
        }
    }

    private static TimeSpan DurationForMediaItem(MediaItem mediaItem)
    {
        if (mediaItem is Image image)
        {
            return TimeSpan.FromSeconds(image.ImageMetadata.Head().DurationSeconds ?? Image.DefaultSeconds);
        }

        MediaVersion version = mediaItem.GetHeadVersion();
        return version.Duration;
    }
}
