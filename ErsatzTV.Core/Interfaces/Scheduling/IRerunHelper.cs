using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IRerunHelper
{
    Task InitWithMediaItems(CollectionKey collectionKey, List<MediaItem> mediaItems);

    IMediaCollectionEnumerator CreateEnumerator(CollectionKey collectionKey, CollectionEnumeratorState state);
}
