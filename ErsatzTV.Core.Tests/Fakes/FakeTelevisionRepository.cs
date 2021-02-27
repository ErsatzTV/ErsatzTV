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
        public Task<bool> Update(Show show) => throw new NotSupportedException();

        public Task<bool> Update(Season season) => throw new NotSupportedException();

        public Task<bool> Update(Episode episode) => throw new NotSupportedException();

        public Task<List<Show>> GetAllShows() => throw new NotSupportedException();

        public Task<Option<Show>> GetShow(int televisionShowId) => throw new NotSupportedException();

        public Task<int> GetShowCount() => throw new NotSupportedException();

        public Task<List<ShowMetadata>> GetPagedShows(int pageNumber, int pageSize) =>
            throw new NotSupportedException();

        public Task<List<Episode>> GetShowItems(int showId) => throw new NotSupportedException();

        public Task<List<Season>> GetAllSeasons() => throw new NotSupportedException();

        public Task<Option<Season>> GetSeason(int televisionSeasonId) => throw new NotSupportedException();

        public Task<int> GetSeasonCount(int televisionShowId) => throw new NotSupportedException();

        public Task<List<Season>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize) =>
            throw new NotSupportedException();

        public Task<List<Episode>> GetSeasonItems(int seasonId) => throw new NotSupportedException();

        public Task<Option<Episode>> GetEpisode(int televisionEpisodeId) => throw new NotSupportedException();

        public Task<int> GetEpisodeCount(int televisionSeasonId) => throw new NotSupportedException();

        public Task<List<EpisodeMetadata>> GetPagedEpisodes(int seasonId, int pageNumber, int pageSize) =>
            throw new NotSupportedException();

        public Task<Option<Show>> GetShowByMetadata(int libraryPathId, ShowMetadata metadata) =>
            throw new NotSupportedException();

        public Task<Either<BaseError, Show>>
            AddShow(int libraryPathId, string showFolder, ShowMetadata metadata) =>
            throw new NotSupportedException();

        public Task<Either<BaseError, Season>> GetOrAddSeason(Show show, string path, int seasonNumber) =>
            throw new NotSupportedException();

        public Task<Either<BaseError, Episode>> GetOrAddEpisode(Season season, LibraryPath libraryPath, string path) =>
            throw new NotSupportedException();

        public Task<Unit> DeleteEmptyShows() => throw new NotSupportedException();

        public Task<Option<Show>> GetShowByPath(int mediaSourceId, string path) => throw new NotSupportedException();

        public Task<Unit> DeleteMissingSources(int localMediaSourceId, List<string> allFolders) =>
            throw new NotSupportedException();
    }
}
