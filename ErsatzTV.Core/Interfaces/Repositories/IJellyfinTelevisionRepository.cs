using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Jellyfin;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IJellyfinTelevisionRepository : IMediaServerTelevisionRepository<JellyfinLibrary, JellyfinShow,
    JellyfinSeason,
    JellyfinEpisode, JellyfinItemEtag>
{
    Task<Option<JellyfinShowTitleItemIdResult>> GetShowTitleItemId(
        int libraryId,
        int showId,
        CancellationToken cancellationToken);
}

public record JellyfinShowTitleItemIdResult(string Title, string ItemId);
