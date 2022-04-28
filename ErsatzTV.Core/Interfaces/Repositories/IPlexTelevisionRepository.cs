using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IPlexTelevisionRepository : IMediaServerTelevisionRepository<PlexLibrary, PlexShow, PlexSeason,
    PlexEpisode, PlexItemEtag>
{
}
