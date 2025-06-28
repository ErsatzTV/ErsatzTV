using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IPlexTelevisionRepository : IMediaServerTelevisionRepository<PlexLibrary, PlexShow, PlexSeason,
    PlexEpisode, PlexItemEtag>
{
    Task<List<int>> RemoveAllTags(PlexLibrary library, PlexTag tag);
    Task<int> AddTag(MediaItem item, PlexTag tag);
}
