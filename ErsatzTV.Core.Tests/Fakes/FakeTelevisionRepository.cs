using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;

namespace ErsatzTV.Core.Tests.Fakes
{
    public class FakeTelevisionRepository : ITelevisionRepository
    {
        public Task<bool> AllShowsExist(List<int> showIds) => throw new NotSupportedException();

        public Task<bool> Update(Show show) => throw new NotSupportedException();

        public Task<bool> Update(Season season) => throw new NotSupportedException();

        public Task<bool> Update(Episode episode) => throw new NotSupportedException();

        public Task<List<Show>> GetAllShows() => throw new NotSupportedException();

        public Task<Option<Show>> GetShow(int showId) => throw new NotSupportedException();

        public Task<int> GetShowCount() => throw new NotSupportedException();

        public Task<List<ShowMetadata>> GetPagedShows(int pageNumber, int pageSize) =>
            throw new NotSupportedException();

        public Task<List<Episode>> GetShowItems(int showId) => throw new NotSupportedException();

        public Task<List<Season>> GetAllSeasons() => throw new NotSupportedException();

        public Task<Option<Season>> GetSeason(int seasonId) => throw new NotSupportedException();

        public Task<int> GetSeasonCount(int showId) => throw new NotSupportedException();

        public Task<List<Season>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize) =>
            throw new NotSupportedException();

        public Task<List<Episode>> GetSeasonItems(int seasonId) => throw new NotSupportedException();

        public Task<Option<Episode>> GetEpisode(int episodeId) => throw new NotSupportedException();

        public Task<int> GetEpisodeCount(int seasonId) => throw new NotSupportedException();

        public Task<List<EpisodeMetadata>> GetPagedEpisodes(int seasonId, int pageNumber, int pageSize) =>
            throw new NotSupportedException();

        public Task<Option<Show>> GetShowByMetadata(int libraryPathId, ShowMetadata metadata) =>
            throw new NotSupportedException();

        public Task<Either<BaseError, Show>>
            AddShow(int libraryPathId, string showFolder, ShowMetadata metadata) =>
            throw new NotSupportedException();

        public Task<Either<BaseError, Season>> GetOrAddSeason(Show show, int libraryPathId, int seasonNumber) =>
            throw new NotSupportedException();

        public Task<Either<BaseError, Episode>> GetOrAddEpisode(Season season, LibraryPath libraryPath, string path) =>
            throw new NotSupportedException();

        public Task<IEnumerable<string>> FindEpisodePaths(LibraryPath libraryPath) => throw new NotSupportedException();

        public Task<Unit> DeleteByPath(LibraryPath libraryPath, string path) => throw new NotSupportedException();

        public Task<Unit> DeleteEmptySeasons(LibraryPath libraryPath) => throw new NotSupportedException();

        public Task<Unit> DeleteEmptyShows(LibraryPath libraryPath) => throw new NotSupportedException();

        public Task<Either<BaseError, PlexShow>> GetOrAddPlexShow(PlexLibrary library, PlexShow item) =>
            throw new NotSupportedException();

        public Task<Either<BaseError, PlexSeason>> GetOrAddPlexSeason(PlexLibrary library, PlexSeason item) =>
            throw new NotSupportedException();

        public Task<Either<BaseError, PlexEpisode>> GetOrAddPlexEpisode(PlexLibrary library, PlexEpisode item) =>
            throw new NotSupportedException();

        public Task<Unit> AddGenre(ShowMetadata metadata, Genre genre) => throw new NotSupportedException();

        public Task<Unit> RemoveMissingPlexShows(PlexLibrary library, List<string> showKeys) =>
            throw new NotSupportedException();

        public Task<Unit> RemoveMissingPlexSeasons(string showKey, List<string> seasonKeys) =>
            throw new NotSupportedException();

        public Task<Unit> RemoveMissingPlexEpisodes(string seasonKey, List<string> episodeKeys) =>
            throw new NotSupportedException();
    }
}
