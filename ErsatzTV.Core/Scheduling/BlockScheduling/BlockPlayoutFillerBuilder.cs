using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using Newtonsoft.Json;

namespace ErsatzTV.Core.Scheduling.BlockScheduling;

public class BlockPlayoutFillerBuilder(
    IMediaCollectionRepository mediaCollectionRepository,
    ITelevisionRepository televisionRepository,
    IArtistRepository artistRepository) : IBlockPlayoutFillerBuilder
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    public async Task<Playout> Build(Playout playout, PlayoutBuildMode mode, CancellationToken cancellationToken)
    {
        if (mode is PlayoutBuildMode.Reset)
        {
            // remove all playout items with type filler
            var toRemove = playout.Items.Where(pi => pi.FillerKind is not FillerKind.None).ToList();
            foreach (PlayoutItem playoutItem in toRemove)
            {
                BlockPlayoutChangeDetection.RemoveItemAndHistory(playout, playoutItem);
            }
        }

        var collectionEnumerators = new Dictionary<CollectionKey, IMediaCollectionEnumerator>();

        // find all unscheduled periods
        var queue = new Queue<PlayoutItem>(playout.Items);
        while (queue.Count > 1)
        {
            PlayoutItem one = queue.Dequeue();
            PlayoutItem two = queue.Peek();

            DateTimeOffset start = one.FinishOffset;
            DateTimeOffset finish = two.StartOffset;

            // find applicable deco
            foreach (Deco deco in GetDecoFor(playout, start))
            {
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
                        playout,
                        deco,
                        historyKey);

                    collectionEnumerators.Add(collectionKey, enumerator);
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
                            MediaItemId = mediaItem.Id,
                            Start = current.UtcDateTime,
                            Finish = current.UtcDateTime + itemDuration,
                            InPoint = TimeSpan.Zero,
                            OutPoint = itemDuration,
                            FillerKind = FillerKind.Fallback,
                            CollectionKey = JsonConvert.SerializeObject(collectionKey, JsonSettings),
                            GuideStart = one.GuideStart,
                            GuideFinish = one.GuideFinish,
                            GuideGroup = one.GuideGroup
                        };

                        if (filler.FinishOffset > finish)
                        {
                            pastTime = true;
                            break;
                        }

                        playout.Items.Add(filler);


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

                        playout.PlayoutHistory.Add(nextHistory);

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


        return playout;
    }

    private static Option<Deco> GetDecoFor(Playout playout, DateTimeOffset start)
    {
        Option<PlayoutTemplate> maybeTemplate = PlayoutTemplateSelector.GetPlayoutTemplateFor(playout.Templates, start);
        foreach (PlayoutTemplate template in maybeTemplate)
        {
            if (template.DecoTemplate is not null)
            {
                foreach (DecoTemplateItem decoTemplateItem in template.DecoTemplate.Items)
                {
                    if (decoTemplateItem.StartTime <= start.TimeOfDay && decoTemplateItem.EndTime > start.TimeOfDay)
                    {
                        switch (decoTemplateItem.Deco.DefaultFillerMode)
                        {
                            case DecoMode.Inherit:
                                return Optional(playout.Deco);
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

        return Optional(playout.Deco);
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
