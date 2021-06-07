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
    public class ProgramScheduleRepository : IProgramScheduleRepository
    {
        private readonly TvContext _dbContext;

        public ProgramScheduleRepository(TvContext dbContext) => _dbContext = dbContext;

        public async Task<ProgramSchedule> Add(ProgramSchedule programSchedule)
        {
            await _dbContext.ProgramSchedules.AddAsync(programSchedule);
            await _dbContext.SaveChangesAsync();
            return programSchedule;
        }

        public Task<Option<ProgramSchedule>> Get(int id) =>
            _dbContext.ProgramSchedules
                .OrderBy(s => s.Id)
                .SingleOrDefaultAsync(s => s.Id == id)
                .Map(Optional);

        public async Task<Option<ProgramSchedule>> GetWithPlayouts(int id) =>
            await _dbContext.ProgramSchedules
                .Include(ps => ps.Items)
                .Include(ps => ps.Playouts)
                .OrderBy(ps => ps.Id)
                .SingleOrDefaultAsync(ps => ps.Id == id);

        public Task<List<ProgramSchedule>> GetAll() =>
            _dbContext.ProgramSchedules.ToListAsync();

        public async Task Update(ProgramSchedule programSchedule)
        {
            _dbContext.ProgramSchedules.Update(programSchedule);
            await _dbContext.SaveChangesAsync();
            await _dbContext.Entry(programSchedule).Collection(s => s.Items).Query().Include(i => i.Collection)
                .LoadAsync();
            await _dbContext.Entry(programSchedule).Collection(s => s.Playouts).LoadAsync();
        }

        public async Task Delete(int programScheduleId)
        {
            ProgramSchedule programSchedule = await _dbContext.ProgramSchedules.FindAsync(programScheduleId);
            _dbContext.ProgramSchedules.Remove(programSchedule);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Option<List<ProgramScheduleItem>>> GetItems(int programScheduleId)
        {
            Option<ProgramSchedule> maybeSchedule = await Get(programScheduleId);
            return await maybeSchedule.Map(
                async programSchedule =>
                {
                    await _dbContext.Entry(programSchedule).Collection(s => s.Items).LoadAsync();
                    await _dbContext.Entry(programSchedule).Collection(s => s.Items).Query()
                        .Include(i => i.Collection)
                        .Include(i => i.MediaItem)
                        .ThenInclude(i => (i as Movie).MovieMetadata)
                        .ThenInclude(mm => mm.Artwork)
                        .Include(i => i.MediaItem)
                        .ThenInclude(i => (i as Season).SeasonMetadata)
                        .ThenInclude(sm => sm.Artwork)
                        .Include(i => i.MediaItem)
                        .ThenInclude(i => (i as Season).Show)
                        .ThenInclude(s => s.ShowMetadata)
                        .ThenInclude(sm => sm.Artwork)
                        .Include(i => i.MediaItem)
                        .ThenInclude(i => (i as Show).ShowMetadata)
                        .ThenInclude(sm => sm.Artwork)
                        .Include(i => i.MediaItem)
                        .ThenInclude(i => (i as Artist).ArtistMetadata)
                        .ThenInclude(am => am.Artwork)
                        .LoadAsync();
                    return programSchedule.Items;
                }).Sequence();
        }
    }
}
