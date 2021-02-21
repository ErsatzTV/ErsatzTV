using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.Data.Sqlite;
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
            _dbContext.Playouts.SingleOrDefaultAsync(p => p.Id == id).Map(Optional);

        public async Task<Option<Playout>> GetFull(int id) =>
            await _dbContext.Playouts
                .Include(p => p.Channel)
                .Include(p => p.Items)
                .Include(p => p.ProgramScheduleAnchors)
                .Include(p => p.ProgramSchedule)
                .ThenInclude(ps => ps.Items)
                .ThenInclude(psi => psi.MediaCollection)
                .Include(p => p.ProgramSchedule)
                .ThenInclude(ps => ps.Items)
                .ThenInclude(psi => psi.TelevisionShow)
                .OrderBy(p => p.Id) // https://github.com/dotnet/efcore/issues/22579#issuecomment-694772289
                .SingleOrDefaultAsync(p => p.Id == id);

        public async Task<Option<PlayoutItem>> GetPlayoutItem(int channelId, DateTimeOffset now)
        {
            var p1 = new SqliteParameter("channelId", channelId);
            var p2 = new SqliteParameter("now", now);
            return await _dbContext.PlayoutItems
                .FromSqlRaw(
                    "select i.* from playoutitems i inner join playouts p on i.playoutid = p.id where p.channelid = @channelId and i.start <= @now and i.finish > @now",
                    p1,
                    p2)
                .Include(i => i.MediaItem)
                .AsNoTracking()
                .SingleOrDefaultAsync();
        }

        public Task<List<PlayoutItem>> GetPlayoutItems(int playoutId) =>
            _dbContext.PlayoutItems
                .Include(i => i.MediaItem)
                .ThenInclude(m => (m as MovieMediaItem).Metadata)
                .Include(i => i.MediaItem)
                .ThenInclude(m => (m as TelevisionEpisodeMediaItem).Metadata)
                .Filter(i => i.PlayoutId == playoutId)
                .ToListAsync();

        public Task<List<int>> GetPlayoutIdsForMediaItems(Seq<MediaItem> mediaItems)
        {
            var ids = string.Join(", ", mediaItems.Map(mi => mi.Id));
            return _dbContext.Playouts.FromSqlRaw(
                @"SELECT DISTINCT p.* FROM Playouts p
INNER JOIN ProgramScheduleItems psi on psi.ProgramScheduleId = p.ProgramScheduleId
INNER JOIN SimpleMediaCollections smc on smc.Id = psi.MediaCollectionId
INNER JOIN MediaItemSimpleMediaCollection mismc on mismc.SimpleMediaCollectionsId = smc.Id
WHERE mismc.ItemsId in ({0})
UNION
SELECT DISTINCT p.* FROM Playouts p
INNER JOIN ProgramScheduleItems psi on psi.ProgramScheduleId = p.ProgramScheduleId
INNER JOIN TelevisionMediaCollections tmc on tmc.Id = psi.MediaCollectionId
INNER JOIN MediaItems mi on mi.Metadata_Title = tmc.ShowTitle and (tmc.SeasonNumber is null or tmc.SeasonNumber = mi.Metadata_SeasonNumber)
WHERE mi.Id in ({0})",
                ids).Select(p => p.Id).ToListAsync();
        }

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
