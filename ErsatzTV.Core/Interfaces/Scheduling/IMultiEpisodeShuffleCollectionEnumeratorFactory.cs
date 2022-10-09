using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Scheduling;

public interface IMultiEpisodeShuffleCollectionEnumeratorFactory
{
    IMediaCollectionEnumerator Create(
        string luaTemplatePath,
        IList<MediaItem> mediaItems,
        CollectionEnumeratorState state);
}
