using System;
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
    public class PlayoutRepository : IPlayoutRepository
    {
        private readonly TvContext _dbContext;

        public PlayoutRepository(TvContext dbContext) => _dbContext = dbContext;

        public async Task<Playout> Add(Playout playout)
        {
            await _dbContext.Playouts.AddAsync(playout);
            await _dbContext.SaveChangesAsync();
            return playout;
        }

        public Task<Option<Playout>> Get(int id) =>
            _dbContext.Playouts
                .OrderBy(p => p.Id)
                .SingleOrDefaultAsync(p => p.Id == id)
                .Map(Optional);

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

        public Task<Option<PlayoutItem>> GetPlayoutItem(int channelId, DateTimeOffset now) =>
            _dbContext.PlayoutItems
                .Where(pi => pi.Playout.ChannelId == channelId)
                .Where(pi => pi.Start <= now.UtcDateTime && pi.Finish > now.UtcDateTime)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Episode).MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Movie).MediaVersions)
                .ThenInclude(mv => mv.MediaFiles)
                .AsNoTracking()
                .SingleOrDefaultAsync()
                .Map(Optional);

        public Task<Option<DateTimeOffset>> GetNextItemStart(int channelId, DateTimeOffset now) =>
            _dbContext.PlayoutItems
                .Where(pi => pi.Playout.ChannelId == channelId)
                .Where(pi => pi.Start > now.UtcDateTime)
                .OrderBy(pi => pi.Start)
                .FirstOrDefaultAsync()
                .Map(Optional)
                .MapT(pi => pi.StartOffset);

        public Task<List<PlayoutItem>> GetPlayoutItems(int playoutId) =>
            _dbContext.PlayoutItems
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Movie).MovieMetadata)
                .ThenInclude(mm => mm.Artwork)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Movie).MediaVersions)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Episode).EpisodeMetadata)
                .ThenInclude(em => em.Artwork)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Episode).MediaVersions)
                .Include(i => i.MediaItem)
                .ThenInclude(mi => (mi as Episode).Season)
                .ThenInclude(s => s.SeasonMetadata)
                .Filter(i => i.PlayoutId == playoutId)
                .ToListAsync();

        public Task<List<Playout>> GetAll() =>
            _dbContext.Playouts
                .Include(p => p.Channel)
                .Include(p => p.ProgramSchedule)
                .ToListAsync();

        public async Task Update(Playout playout)
        {
            _dbContext.Playouts.Update(playout);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(int playoutId)
        {
            Playout playout = await _dbContext.Playouts.FindAsync(playoutId);
            _dbContext.Playouts.Remove(playout);
            await _dbContext.SaveChangesAsync();
        }
    }
}
