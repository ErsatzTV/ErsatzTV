using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class FFmpegProfileRepository : IFFmpegProfileRepository
    {
        private readonly TvContext _dbContext;
        private readonly IDbContextFactory<TvContext> _dbContextFactory;

        public FFmpegProfileRepository(IDbContextFactory<TvContext> dbContextFactory, TvContext dbContext)
        {
            _dbContextFactory = dbContextFactory;
            _dbContext = dbContext;
        }

        public async Task<FFmpegProfile> Add(FFmpegProfile ffmpegProfile)
        {
            await _dbContext.FFmpegProfiles.AddAsync(ffmpegProfile);
            await _dbContext.SaveChangesAsync();
            return ffmpegProfile;
        }

        public async Task<Option<FFmpegProfile>> Get(int id) =>
            await _dbContext.FFmpegProfiles
                .Include(p => p.Resolution)
                .OrderBy(p => p.Id)
                .SingleOrDefaultAsync(p => p.Id == id);

        public Task<List<FFmpegProfile>> GetAll() =>
            _dbContext.FFmpegProfiles
                .Include(p => p.Resolution)
                .ToListAsync();

        public Task Update(FFmpegProfile ffmpegProfile)
        {
            _dbContext.FFmpegProfiles.Update(ffmpegProfile);
            return _dbContext.SaveChangesAsync();
        }

        public async Task Delete(int ffmpegProfileId)
        {
            FFmpegProfile ffmpegProfile = await _dbContext.FFmpegProfiles.FindAsync(ffmpegProfileId);
            _dbContext.FFmpegProfiles.Remove(ffmpegProfile);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<FFmpegProfile> Copy(int ffmpegProfileId, string name)
        {
            await using TvContext dbContext = _dbContextFactory.CreateDbContext();
            FFmpegProfile ffmpegProfile = await dbContext.FFmpegProfiles.FindAsync(ffmpegProfileId);

            PropertyValues values = dbContext.Entry(ffmpegProfile).CurrentValues.Clone();
            values["Id"] = 0;

            var clone = new FFmpegProfile();
            await dbContext.AddAsync(clone);
            dbContext.Entry(clone).CurrentValues.SetValues(values);
            clone.Name = name;

            await dbContext.SaveChangesAsync();
            await dbContext.Entry(clone).Reference(f => f.Resolution).LoadAsync();
                
            return clone;
        }
    }
}
