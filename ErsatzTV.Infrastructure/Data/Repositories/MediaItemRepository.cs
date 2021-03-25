using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class MediaItemRepository : IMediaItemRepository
    {
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public MediaItemRepository(IDbContextFactory<TvContext> dbContextFactory) =>
            _dbContextFactory = dbContextFactory;

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
    }
}
