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

    public override async Task<Either<BaseError, PlayoutBuildResult>> Build(
        DateTimeOffset start,
        Playout playout,
        PlayoutReferenceData referenceData,
        PlayoutBuildMode mode,
        CancellationToken cancellationToken)
    {
        _randomizedCollections.Add(playout.Channel.UniqueId, []);

        Either<BaseError, PlayoutBuildResult> buildResult = await base.Build(
            start,
            playout,
            referenceData,
            mode,
            cancellationToken);

        _randomizedCollections.Remove(playout.Channel.UniqueId);

        return buildResult;
    }

    protected override Task<int> GetDaysToBuild(CancellationToken cancellationToken) => Task.FromResult(1);

    protected override IMediaCollectionEnumerator GetEnumerator(
        Playout playout,
        PlayoutReferenceData referenceData,
        PlayoutBuildResult result,
        BlockItem blockItem,
        DateTimeOffset currentTime,
        string historyKey,
        Map<CollectionKey, List<MediaItem>> collectionMediaItems)
    {
        IMediaCollectionEnumerator enumerator = base.GetEnumerator(
            playout,
            referenceData,
            result,
            blockItem,
            currentTime,
            historyKey,
            collectionMediaItems);

        var collectionKey = CollectionKey.ForBlockItem(blockItem);
        if (!_randomizedCollections[referenceData.Channel.UniqueId].Contains(collectionKey))
        {
            enumerator.ResetState(
                new CollectionEnumeratorState
                {
                    Seed = new Random().Next(),
                    Index = new Random().Next(collectionMediaItems[collectionKey].Count)
                });

            _randomizedCollections[referenceData.Channel.UniqueId].Add(collectionKey);
        }

        return enumerator;
    }
}
