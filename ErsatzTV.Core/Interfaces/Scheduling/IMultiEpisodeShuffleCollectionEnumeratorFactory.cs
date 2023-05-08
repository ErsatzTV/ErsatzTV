using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IMultiEpisodeShuffleCollectionEnumeratorFactory
{
    IMediaCollectionEnumerator Create(
        string jsScriptPath,
        IList<MediaItem> mediaItems,
        CollectionEnumeratorState state,
        CancellationToken cancellationToken);
}
