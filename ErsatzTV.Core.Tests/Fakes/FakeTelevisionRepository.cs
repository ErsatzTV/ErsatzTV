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
        public Task<bool> Update(TelevisionShow show) => throw new NotSupportedException();

        public Task<bool> Update(TelevisionSeason season) => throw new NotSupportedException();

        public Task<bool> Update(TelevisionEpisodeMediaItem episode) => throw new NotSupportedException();

        public Task<List<TelevisionShow>> GetAllShows() => throw new NotSupportedException();

        public Task<Option<TelevisionShow>> GetShow(int televisionShowId) => throw new NotSupportedException();

        public Task<int> GetShowCount() => throw new NotSupportedException();

        public Task<List<TelevisionShow>> GetPagedShows(int pageNumber, int pageSize) =>
            throw new NotSupportedException();

        public Task<List<TelevisionEpisodeMediaItem>> GetShowItems(int televisionShowId) =>
            throw new NotSupportedException();

        public Task<List<TelevisionSeason>> GetAllSeasons() => throw new NotSupportedException();

        public Task<Option<TelevisionSeason>> GetSeason(int televisionSeasonId) => throw new NotSupportedException();

        public Task<int> GetSeasonCount(int televisionShowId) => throw new NotSupportedException();

        public Task<List<TelevisionSeason>> GetPagedSeasons(int televisionShowId, int pageNumber, int pageSize) =>
            throw new NotSupportedException();

        public Task<List<TelevisionEpisodeMediaItem>> GetSeasonItems(int televisionSeasonId) =>
            throw new NotSupportedException();

        public Task<Option<TelevisionEpisodeMediaItem>> GetEpisode(int televisionEpisodeId) =>
            throw new NotSupportedException();

        public Task<int> GetEpisodeCount(int televisionSeasonId) => throw new NotSupportedException();

        public Task<List<TelevisionEpisodeMediaItem>> GetPagedEpisodes(
            int televisionSeasonId,
            int pageNumber,
            int pageSize) => throw new NotSupportedException();

        public Task<Option<TelevisionShow>> GetShowByPath(int mediaSourceId, string path) =>
            throw new NotSupportedException();

        public Task<Option<TelevisionShow>> GetShowByMetadata(TelevisionShowMetadata metadata) =>
            throw new NotSupportedException();

        public Task<Either<BaseError, TelevisionShow>> AddShow(
            int localMediaSourceId,
            string showFolder,
            TelevisionShowMetadata metadata) => throw new NotSupportedException();

        public Task<Either<BaseError, TelevisionSeason>> GetOrAddSeason(
            TelevisionShow show,
            string path,
            int seasonNumber) => throw new NotSupportedException();

        public Task<Either<BaseError, TelevisionEpisodeMediaItem>> GetOrAddEpisode(
            TelevisionSeason season,
            LibraryPath libraryPath,
            string path) => throw new NotSupportedException();

        public Task<Unit> DeleteMissingSources(int localMediaSourceId, List<string> allFolders) =>
            throw new NotSupportedException();

        public Task<Unit> DeleteEmptyShows() => throw new NotSupportedException();
    }
}
