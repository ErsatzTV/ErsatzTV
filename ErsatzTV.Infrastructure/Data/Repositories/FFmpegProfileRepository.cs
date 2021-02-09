using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Data.Repositories
{
    public class FFmpegProfileRepository : IFFmpegProfileRepository
    {
        private readonly TvContext _dbContext;

        public FFmpegProfileRepository(TvContext dbContext) => _dbContext = dbContext;

        public async Task<FFmpegProfile> Add(FFmpegProfile ffmpegProfile)
        {
            await _dbContext.FFmpegProfiles.AddAsync(ffmpegProfile);
            await _dbContext.SaveChangesAsync();
            return ffmpegProfile;
        }

        public async Task<Option<FFmpegProfile>> Get(int id) =>
            await _dbContext.FFmpegProfiles
                .Include(p => p.Resolution)
                .SingleOrDefaultAsync(p => p.Id == id);

        public Task<List<FFmpegProfile>> GetAll() =>
            _dbContext.FFmpegProfiles
                .Include(p => p.Resolution)
                .ToListAsync();

        public async Task Update(FFmpegProfile ffmpegProfile)
        {
            _dbContext.FFmpegProfiles.Update(ffmpegProfile);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(int ffmpegProfileId)
        {
            FFmpegProfile ffmpegProfile = await _dbContext.FFmpegProfiles.FindAsync(ffmpegProfileId);
            _dbContext.FFmpegProfiles.Remove(ffmpegProfile);
            await _dbContext.SaveChangesAsync();
        }
    }
}
