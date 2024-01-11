using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;

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
    Task<Option<Show>> GetShowByMetadata(int libraryPathId, ShowMetadata metadata, string showFolder);
    Task<Either<BaseError, MediaItemScanResult<Show>>> AddShow(int libraryPathId, ShowMetadata metadata);
    Task<Either<BaseError, Season>> GetOrAddSeason(Show show, int libraryPathId, int seasonNumber);
    Task<Either<BaseError, Episode>> GetOrAddEpisode(Season season, LibraryPath libraryPath, string path);
    Task<IEnumerable<string>> FindEpisodePaths(LibraryPath libraryPath);
    Task<Unit> DeleteByPath(LibraryPath libraryPath, string path);
    Task<Unit> DeleteEmptySeasons(LibraryPath libraryPath);
    Task<List<int>> DeleteEmptyShows(LibraryPath libraryPath);
    Task<bool> AddGenre(ShowMetadata metadata, Genre genre);
    Task<bool> AddGenre(EpisodeMetadata metadata, Genre genre);
    Task<bool> AddTag(Domain.Metadata metadata, Tag tag);
    Task<bool> AddStudio(ShowMetadata metadata, Studio studio);
    Task<bool> AddActor(ShowMetadata metadata, Actor actor);
    Task<bool> AddActor(EpisodeMetadata metadata, Actor actor);
    Task<Unit> RemoveMetadata(Episode episode, EpisodeMetadata metadata);
    Task<bool> AddDirector(EpisodeMetadata metadata, Director director);
    Task<bool> AddWriter(EpisodeMetadata metadata, Writer writer);
    Task<bool> UpdateTitles(EpisodeMetadata metadata, string title, string sortTitle);
    Task<bool> UpdateOutline(EpisodeMetadata metadata, string outline);
    Task<bool> UpdatePlot(EpisodeMetadata metadata, string plot);
    Task<bool> UpdateYear(ShowMetadata metadata, int? year);
}
