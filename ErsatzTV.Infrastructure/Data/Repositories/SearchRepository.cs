using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class SearchRepository : ISearchRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public SearchRepository(IDbContextFactory<TvContext> dbContextFactory, IDbConnection dbConnection)
        {
            _dbContextFactory = dbContextFactory;
            _dbConnection = dbConnection;
        }

        public Task<List<int>> GetItemIdsToIndex() =>
            _dbConnection.QueryAsync<int>(@"SELECT Id FROM MediaItem")
                .Map(result => result.ToList());

        public async Task<Option<MediaItem>> GetItemToIndex(int id)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.MediaItems
                .AsNoTracking()
                .Include(mi => mi.LibraryPath)
                .ThenInclude(lp => lp.Library)
                .Include(mi => (mi as Movie).MovieMetadata)
                .ThenInclude(mm => mm.Genres)
                .Include(mi => (mi as Movie).MovieMetadata)
                .ThenInclude(mm => mm.Tags)
                .Include(mi => (mi as Movie).MovieMetadata)
                .ThenInclude(mm => mm.Studios)
                .Include(mi => (mi as Movie).MediaVersions)
                .ThenInclude(mm => mm.Streams)
                .Include(mi => (mi as Show).ShowMetadata)
                .ThenInclude(mm => mm.Genres)
                .Include(mi => (mi as Show).ShowMetadata)
                .ThenInclude(mm => mm.Tags)
                .Include(mi => (mi as Show).ShowMetadata)
                .ThenInclude(mm => mm.Studios)
                .Include(mi => (mi as MusicVideo).MusicVideoMetadata)
                .ThenInclude(mm => mm.Genres)
                .Include(mi => (mi as MusicVideo).MusicVideoMetadata)
                .ThenInclude(mm => mm.Tags)
                .Include(mi => (mi as MusicVideo).MusicVideoMetadata)
                .ThenInclude(mm => mm.Studios)
                .Include(mi => (mi as MusicVideo).MediaVersions)
                .ThenInclude(mm => mm.Streams)
                .OrderBy(mi => mi.Id)
                .SingleOrDefaultAsync(mi => mi.Id == id)
                .Map(Optional);
        }

        public Task<List<string>> GetLanguagesForShow(Show show) =>
            _dbConnection.QueryAsync<string>(
                @"SELECT DISTINCT Language
                    FROM MediaStream
                    INNER JOIN MediaVersion MV ON MediaStream.MediaVersionId = MV.Id
                    INNER JOIN Episode E ON MV.EpisodeId = E.Id
                    INNER JOIN Season S ON E.SeasonId = S.Id
                    WHERE MediaStreamKind = 2 AND S.ShowId = @ShowId",
                new { ShowId = show.Id }).Map(result => result.ToList());

        public Task<List<string>> GetLanguagesForArtist(Artist artist) =>
            _dbConnection.QueryAsync<string>(
                @"SELECT DISTINCT Language
                    FROM MediaStream
                    INNER JOIN MediaVersion V ON MediaStream.MediaVersionId = V.Id
                    INNER JOIN MusicVideo MV ON V.MusicVideoId = MV.Id
                    INNER JOIN Artist A on MV.ArtistId = A.Id
                    WHERE MediaStreamKind = 2 AND A.Id = @ArtistId",
                new { ArtistId = artist.Id }).Map(result => result.ToList());
    }
}
