using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Scheduling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ErsatzTV.Core.Scheduling.BlockScheduling;

public class BlockPlayoutPreviewBuilder(
    IConfigElementRepository configElementRepository,
    IMediaCollectionRepository mediaCollectionRepository,
    ITelevisionRepository televisionRepository,
    IArtistRepository artistRepository,
    ILogger<BlockPlayoutBuilder> logger) : BlockPlayoutBuilder(
    configElementRepository,
    mediaCollectionRepository,
    televisionRepository,
    artistRepository,
    logger), IBlockPlayoutPreviewBuilder
{
    protected override ILogger Logger => NullLogger.Instance;

    protected override Task<int> GetDaysToBuild() => Task.FromResult(1);

    protected override IMediaCollectionEnumerator GetEnumerator(
        Playout playout,
        BlockItem blockItem,
        DateTimeOffset currentTime,
        string historyKey,
        Map<CollectionKey, List<MediaItem>> collectionMediaItems)
    {
        IMediaCollectionEnumerator enumerator = base.GetEnumerator(playout, blockItem, currentTime, historyKey, collectionMediaItems);
        
        var collectionKey = CollectionKey.ForBlockItem(blockItem);
        
        enumerator.ResetState(
            new CollectionEnumeratorState
            {
                Seed = new Random().Next(),
                Index = new Random().Next(collectionMediaItems[collectionKey].Count)
            });

        return enumerator;
    }
}
