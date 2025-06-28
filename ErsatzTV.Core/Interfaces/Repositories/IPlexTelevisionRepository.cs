using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IPlexTelevisionRepository : IMediaServerTelevisionRepository<PlexLibrary, PlexShow, PlexSeason,
    PlexEpisode, PlexItemEtag>
{
    Task<List<int>> RemoveAllTags(PlexLibrary library, PlexTag tag, System.Collections.Generic.HashSet<int> keep);
    Task<PlexShowAddTagResult> AddTag(PlexShow show, PlexTag tag);
}

public record PlexShowAddTagResult(Option<int> Existing, Option<int> Added);
