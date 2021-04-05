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
    public class MediaItemRepository : IMediaItemRepository
    {
        private readonly IDbConnection _dbConnection;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public MediaItemRepository(IDbContextFactory<TvContext> dbContextFactory, IDbConnection dbConnection)
        {
            _dbContextFactory = dbContextFactory;
            _dbConnection = dbConnection;
        }

        public async Task<Option<MediaItem>> Get(int id)
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            return await context.MediaItems
                .Include(i => i.LibraryPath)
                .OrderBy(i => i.Id)
                .SingleOrDefaultAsync(i => i.Id == id)
                .Map(Optional);
        }

        public async Task<List<MediaItem>> GetAll()
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            return await context.MediaItems.ToListAsync();
        }

        public async Task<bool> Update(MediaItem mediaItem)
        {
            await using TvContext context = _dbContextFactory.CreateDbContext();
            context.MediaItems.Update(mediaItem);
            return await context.SaveChangesAsync() > 0;
        }

        public Task<List<string>> GetAllLanguageCodes() =>
            _dbConnection.QueryAsync<string>(
                    @"SELECT LanguageCode FROM
                    (SELECT Language AS LanguageCode
                    FROM MediaStream WHERE Language IS NOT NULL
                    UNION ALL SELECT PreferredLanguageCode AS LanguageCode
                    FROM Channel WHERE PreferredLanguageCode IS NOT NULL)
                    GROUP BY LanguageCode
                    ORDER BY COUNT(LanguageCode) DESC")
                .Map(result => result.ToList());
    }
}
