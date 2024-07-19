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

    public async Task<Playout> Build(Playout playout, CancellationToken cancellationToken)
    {
        // remove all playout items with type filler
        playout.Items.RemoveAll(pi => pi.FillerKind is not FillerKind.None);

        var collectionMediaItems = new Dictionary<CollectionKey, List<MediaItem>>();

        // find all unscheduled periods
        var queue = new Queue<PlayoutItem>(playout.Items);
        while (queue.Count > 1)
        {
            PlayoutItem one = queue.Dequeue();
            PlayoutItem two = queue.Peek();

            DateTimeOffset start = one.FinishOffset;
            DateTimeOffset finish = two.Start;

            // find applicable deco
            foreach (Deco deco in GetDecoFor(playout, start))
            {
                var collectionKey = CollectionKey.ForDecoDefaultFiller(deco);

                // load collection items from db on demand
                if (!collectionMediaItems.TryGetValue(collectionKey, out List<MediaItem> items))
                {
                    items = await MediaItemsForCollection.Collect(
                        mediaCollectionRepository,
                        televisionRepository,
                        artistRepository,
                        collectionKey);

                    collectionMediaItems.Add(collectionKey, items);
                }

                DateTimeOffset current = start;
                var pastTime = false;
                while (current < finish)
                {
                    var enumerator = new RandomizedMediaCollectionEnumerator(
                        items,
                        new CollectionEnumeratorState { Index = 0, Seed = 0 });

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
                            CollectionKey = JsonConvert.SerializeObject(collectionKey, JsonSettings)
                        };

                        if (filler.FinishOffset > finish)
                        {
                            logger.LogDebug("Filler would run into primary content; done scheduling this period");
                            pastTime = true;
                            break;
                        }

                        playout.Items.Add(filler);

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
