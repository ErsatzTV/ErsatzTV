using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;
using ErsatzTV.Core.Plex;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface ITelevisionRepository
{
    Task<bool> AllShowsExist(List<int> showIds);
    Task<bool> AllSeasonsExist(List<int> seasonIds);
    Task<bool> AllEpisodesExist(List<int> episodeIds);
    Task<List<Show>> GetAllShows();
    Task<Option<Show>> GetShow(int showId);
    Task<List<ShowMetadata>> GetShowsForCards(List<int> ids);
    Task<List<SeasonMetadata>> GetSeasonsForCards(List<int> ids);
    Task<List<EpisodeMetadata>> GetEpisodesForCards(List<int> ids);
    Task<List<Episode>> GetShowItems(int showId);
    Task<List<Season>> GetAllSeasons();
    Task<Option<Season>> GetSeason(int seasonId);
    Task<int> GetSeasonCount(int showId);
    Task<List<Season>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize);
    Task<List<Episode>> GetSeasonItems(int seasonId);
    Task<int> GetEpisodeCount(int seasonId);
    Task<List<EpisodeMetadata>> GetPagedEpisodes(int seasonId, int pageNumber, int pageSize);
    Task<Option<Show>> GetShowByMetadata(int libraryPathId, ShowMetadata metadata);

    Task<Either<BaseError, MediaItemScanResult<Show>>> AddShow(
        int libraryPathId,
        string showFolder,
        ShowMetadata metadata);

    Task<Either<BaseError, Season>> GetOrAddSeason(Show show, int libraryPathId, int seasonNumber);
    Task<Either<BaseError, Episode>> GetOrAddEpisode(Season season, LibraryPath libraryPath, string path);
    Task<IEnumerable<string>> FindEpisodePaths(LibraryPath libraryPath);
    Task<Unit> DeleteByPath(LibraryPath libraryPath, string path);
    Task<Unit> DeleteEmptySeasons(LibraryPath libraryPath);
    Task<List<int>> DeleteEmptyShows(LibraryPath libraryPath);
    Task<Either<BaseError, MediaItemScanResult<PlexShow>>> GetOrAddPlexShow(PlexLibrary library, PlexShow item);
    Task<Either<BaseError, PlexSeason>> GetOrAddPlexSeason(PlexLibrary library, PlexSeason item);

    Task<Either<BaseError, MediaItemScanResult<PlexEpisode>>>
        GetOrAddPlexEpisode(PlexLibrary library, PlexEpisode item);

    Task<bool> AddGenre(ShowMetadata metadata, Genre genre);
    Task<bool> AddTag(Domain.Metadata metadata, Tag tag);
    Task<bool> AddStudio(ShowMetadata metadata, Studio studio);
    Task<bool> AddActor(ShowMetadata metadata, Actor actor);
    Task<bool> AddActor(EpisodeMetadata metadata, Actor actor);
    Task<List<int>> RemoveMissingPlexShows(PlexLibrary library, List<string> showKeys);
    Task<Unit> RemoveMissingPlexSeasons(string showKey, List<string> seasonKeys);
    Task<List<int>> RemoveMissingPlexEpisodes(string seasonKey, List<string> episodeKeys);
    Task<Unit> RemoveMetadata(Episode episode, EpisodeMetadata metadata);
    Task<bool> AddDirector(EpisodeMetadata metadata, Director director);
    Task<bool> AddWriter(EpisodeMetadata metadata, Writer writer);
    Task<Unit> UpdatePath(int mediaFileId, string path);
    Task<Unit> SetPlexEtag(PlexShow show, string etag);
    Task<Unit> SetPlexEtag(PlexSeason season, string etag);
    Task<Unit> SetPlexEtag(PlexEpisode episode, string etag);
    Task<List<PlexItemEtag>> GetExistingPlexEpisodes(PlexLibrary library, PlexSeason season);
}
