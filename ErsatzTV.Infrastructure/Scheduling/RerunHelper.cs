using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Infrastructure.Scheduling;

public class RerunHelper : IRerunHelper
{
    private readonly Dictionary<CollectionKey, List<MediaItem>> _mediaItems = new();

    public async Task InitWithMediaItems(CollectionKey collectionKey, List<MediaItem> mediaItems)
    {
        _mediaItems.TryAdd(collectionKey, mediaItems);

        // TODO: load history
        await Task.Delay(10);
    }

    public IMediaCollectionEnumerator CreateEnumerator(CollectionKey collectionKey, CollectionEnumeratorState state)
    {
        switch (collectionKey.CollectionType)
        {
            case CollectionType.RerunFirstRun:
                return new RandomizedMediaCollectionEnumerator(_mediaItems[collectionKey], state);
            case CollectionType.RerunRerun:
                return new RandomizedMediaCollectionEnumerator(_mediaItems[collectionKey], state);
            default:
                throw new ArgumentOutOfRangeException(nameof(collectionKey));
        }
    }
}
