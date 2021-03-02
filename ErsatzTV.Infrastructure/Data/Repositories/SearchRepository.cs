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

        public async Task<List<MediaItem>> SearchMediaItems(string query)
        {
            List<int> ids = await _dbConnection.QueryAsync<int>(
                    @"SELECT M.Id FROM Movie M
                    INNER JOIN MovieMetadata MM on M.Id = MM.MovieId
                    WHERE MM.Title LIKE @Query
                    UNION
                    SELECT S.Id FROM Show S
                    INNER JOIN ShowMetadata SM on S.Id = SM.ShowId
                    WHERE SM.Title LIKE @Query",
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
    }
}
