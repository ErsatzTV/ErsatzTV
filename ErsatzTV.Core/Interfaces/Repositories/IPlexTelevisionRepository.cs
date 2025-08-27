using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IPlexTelevisionRepository : IMediaServerTelevisionRepository<PlexLibrary, PlexShow, PlexSeason,
    PlexEpisode, PlexItemEtag>
{
    Task<List<int>> RemoveAllTags(
        PlexLibrary library,
        PlexTag tag,
        System.Collections.Generic.HashSet<int> keep,
        CancellationToken cancellationToken);
    Task<PlexShowAddTagResult> AddTag(
        PlexLibrary library,
        PlexShow show,
        PlexTag tag,
        CancellationToken cancellationToken);
    Task UpdateLastNetworksScan(PlexLibrary library, CancellationToken cancellationToken);
    Task<Option<PlexShowTitleKeyResult>> GetShowTitleKey(int libraryId, int showId, CancellationToken cancellationToken);
}

public record PlexShowAddTagResult(Option<int> Existing, Option<int> Added);

public record PlexShowTitleKeyResult(string Title, string Key);
