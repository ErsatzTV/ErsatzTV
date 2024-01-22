using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Core.Interfaces.Metadata;
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
    ICollectionEtag collectionEtag,
    ILogger<BlockPlayoutBuilder> logger) : BlockPlayoutBuilder(
    configElementRepository,
    mediaCollectionRepository,
    televisionRepository,
    artistRepository,
    collectionEtag,
    logger), IBlockPlayoutPreviewBuilder
{
    private readonly Dictionary<Guid, System.Collections.Generic.HashSet<CollectionKey>> _randomizedCollections = [];

    protected override ILogger Logger => NullLogger.Instance;

    public override async Task<Playout> Build(
        Playout playout,
        PlayoutBuildMode mode,
        CancellationToken cancellationToken)
    {
        _randomizedCollections.Add(playout.Channel.UniqueId, []);

        Playout result = await base.Build(playout, mode, cancellationToken);

        _randomizedCollections.Remove(playout.Channel.UniqueId);

        return result;
    }

    protected override Task<int> GetDaysToBuild() => Task.FromResult(1);

    protected override IMediaCollectionEnumerator GetEnumerator(
        Playout playout,
        BlockItem blockItem,
        DateTimeOffset currentTime,
        string historyKey,
        Map<CollectionKey, List<MediaItem>> collectionMediaItems)
    {
        IMediaCollectionEnumerator enumerator = base.GetEnumerator(
            playout,
            blockItem,
            currentTime,
            historyKey,
            collectionMediaItems);

        var collectionKey = CollectionKey.ForBlockItem(blockItem);
        if (!_randomizedCollections[playout.Channel.UniqueId].Contains(collectionKey))
        {
            enumerator.ResetState(
                new CollectionEnumeratorState
                {
                    Seed = new Random().Next(),
                    Index = new Random().Next(collectionMediaItems[collectionKey].Count)
                });

            _randomizedCollections[playout.Channel.UniqueId].Add(collectionKey);
        }

        return enumerator;
    }
}
