using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IRerunHelper
{
    Task InitWithMediaItems(
        int playoutId,
        CollectionKey collectionKey,
        List<MediaItem> mediaItems,
        CancellationToken cancellationToken);

    IMediaCollectionEnumerator CreateEnumerator(CollectionKey collectionKey, CollectionEnumeratorState state);

    bool IsFirstRun(CollectionKey collectionKey, int mediaItemId);

    bool IsRerun(CollectionKey collectionKey, int mediaItemId);

    int FirstRunCount(CollectionKey collectionKey);

    int RerunCount(CollectionKey collectionKey);

    void AddToHistory(CollectionKey collectionKey, int mediaItemId);
}
