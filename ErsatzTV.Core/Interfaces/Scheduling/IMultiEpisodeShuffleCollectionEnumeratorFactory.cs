using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IMultiEpisodeShuffleCollectionEnumeratorFactory
{
    IMediaCollectionEnumerator Create(
        string luaScriptPath,
        IList<MediaItem> mediaItems,
        CollectionEnumeratorState state);
}
