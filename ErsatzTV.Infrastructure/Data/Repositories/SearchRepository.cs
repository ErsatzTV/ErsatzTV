using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

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

        public async Task<List<MediaItem>> GetItemsToIndex()
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.MediaItems
                .AsNoTracking()
                .Include(mi => (mi as Movie).MovieMetadata)
                .ThenInclude(mm => mm.Genres)
                .Include(mi => (mi as Movie).MovieMetadata)
                .ThenInclude(mm => mm.Tags)
                .Include(mi => (mi as Show).ShowMetadata)
                .ThenInclude(mm => mm.Genres)
                .Include(mi => (mi as Show).ShowMetadata)
                .ThenInclude(mm => mm.Tags)
                .ToListAsync();
        }

        public async Task<List<MediaItem>> SearchMediaItemsByTitle(string query)
        {
            List<int> ids = await _dbConnection.QueryAsync<int>(
                    @"SELECT M.Id FROM Movie M
                    INNER JOIN MovieMetadata MM on M.Id = MM.MovieId
                    WHERE MM.Title LIKE @Query
                    UNION
                    SELECT S.Id FROM Show S
                    INNER JOIN ShowMetadata SM on S.Id = SM.ShowId
                    WHERE SM.Title LIKE @Query
                    GROUP BY SM.Title, SM.Year",
                    new { Query = $"%{query}%" })
                .Map(results => results.ToList());

            await using TvContext context = _dbContextFactory.CreateDbContext();
            return await context.MediaItems
                .Filter(m => ids.Contains(m.Id))
                .Include(m => (m as Movie).MovieMetadata)
                .ThenInclude(mm => mm.Artwork)
                .Include(m => (m as Show).ShowMetadata)
                .ThenInclude(mm => mm.Artwork)
                .OfType<MediaItem>()
                .ToListAsync();
        }

        public async Task<List<MediaItem>> SearchMediaItemsByGenre(string genre)
        {
            List<int> ids = await _dbConnection.QueryAsync<int>(
                    @"SELECT M.Id FROM Movie M
                    INNER JOIN MovieMetadata MM on M.Id = MM.MovieId
                    INNER JOIN Genre G on MM.Id = G.MovieMetadataId
                    WHERE G.Name LIKE @Query
                    UNION
                    SELECT S.Id FROM Show S
                    INNER JOIN ShowMetadata SM on S.Id = SM.ShowId
                    INNER JOIN Genre G2 on SM.Id = G2.ShowMetadataId
                    WHERE G2.Name LIKE @Query
                    GROUP BY SM.Title, SM.Year",
                    new { Query = genre })
                .Map(results => results.ToList());

            await using TvContext context = _dbContextFactory.CreateDbContext();
            return await context.MediaItems
                .Filter(m => ids.Contains(m.Id))
                .Include(m => (m as Movie).MovieMetadata)
                .ThenInclude(mm => mm.Artwork)
                .Include(m => (m as Show).ShowMetadata)
                .ThenInclude(mm => mm.Artwork)
                .OfType<MediaItem>()
                .ToListAsync();
        }

        public async Task<List<MediaItem>> SearchMediaItemsByTag(string tag)
        {
            List<int> ids = await _dbConnection.QueryAsync<int>(
                    @"SELECT M.Id FROM Movie M
                    INNER JOIN MovieMetadata MM on M.Id = MM.MovieId
                    INNER JOIN Tag T on MM.Id = T.MovieMetadataId
                    WHERE T.Name LIKE @Query
                    UNION
                    SELECT S.Id FROM Show S
                    INNER JOIN ShowMetadata SM on S.Id = SM.ShowId
                    INNER JOIN Tag T2 on SM.Id = T2.ShowMetadataId
                    WHERE T2.Name LIKE @Query
                    GROUP BY SM.Title, SM.Year",
                    new { Query = tag })
                .Map(results => results.ToList());

            await using TvContext context = _dbContextFactory.CreateDbContext();
            return await context.MediaItems
                .Filter(m => ids.Contains(m.Id))
                .Include(m => (m as Movie).MovieMetadata)
                .ThenInclude(mm => mm.Artwork)
                .Include(m => (m as Show).ShowMetadata)
                .ThenInclude(mm => mm.Artwork)
                .OfType<MediaItem>()
                .ToListAsync();
        }
    }
}
