using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Jellyfin;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IJellyfinTelevisionRepository : IMediaServerTelevisionRepository<JellyfinLibrary, JellyfinShow,
    JellyfinSeason,
    JellyfinEpisode, JellyfinItemEtag>
{
}
