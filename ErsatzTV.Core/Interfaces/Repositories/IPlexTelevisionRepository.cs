using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IPlexTelevisionRepository
{
    Task<List<PlexItemEtag>> GetExistingPlexShows(PlexLibrary library);
    Task<List<PlexItemEtag>> GetExistingPlexSeasons(PlexLibrary library, PlexShow show);
    Task<List<PlexItemEtag>> GetExistingPlexEpisodes(PlexLibrary library, PlexSeason season);
    Task<bool> FlagNormal(PlexLibrary library, PlexEpisode episode);
    Task<Option<int>> FlagUnavailable(PlexLibrary library, PlexEpisode episode);
    Task<List<int>> FlagFileNotFoundShows(PlexLibrary library, List<string> plexShowKeys);
    Task<List<int>> FlagFileNotFoundSeasons(PlexLibrary library, List<string> plexSeasonKeys);
    Task<List<int>> FlagFileNotFoundEpisodes(PlexLibrary library, List<string> plexEpisodeKeys);
    Task<Unit> SetPlexEtag(PlexShow show, string etag);
    Task<Unit> SetPlexEtag(PlexSeason season, string etag);
    Task<Unit> SetPlexEtag(PlexEpisode episode, string etag);
}
