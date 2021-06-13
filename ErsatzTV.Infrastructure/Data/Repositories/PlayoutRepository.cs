using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class PlayoutRepository : IPlayoutRepository
    {
        private readonly TvContext _dbContext;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public PlayoutRepository(TvContext dbContext, IDbContextFactory<TvContext> dbContextFactory)
        {
            _dbContext = dbContext;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<Option<Playout>> GetFull(int id) =>
            await _dbContext.Playouts
                .Include(p => p.Channel)
                .Include(p => p.Items)
                .Include(p => p.ProgramScheduleAnchors)
                .ThenInclude(a => a.MediaItem)
                .Include(p => p.ProgramSchedule)
                .ThenInclude(ps => ps.Items)
                .ThenInclude(psi => psi.Collection)
                .Include(p => p.ProgramSchedule)
                .ThenInclude(ps => ps.Items)
                .ThenInclude(psi => psi.MediaItem)
                .OrderBy(p => p.Id) // https://github.com/dotnet/efcore/issues/22579#issuecomment-694772289
                .SingleOrDefaultAsync(p => p.Id == id);

        public Task Update(Playout playout)
        {
            _dbContext.Playouts.Update(playout);
            return _dbContext.SaveChangesAsync();
        }
    }
}
