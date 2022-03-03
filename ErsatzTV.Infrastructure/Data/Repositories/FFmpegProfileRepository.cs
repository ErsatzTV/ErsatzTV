using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ErsatzTV.Infrastructure.Data.Repositories;

public class FFmpegProfileRepository : IFFmpegProfileRepository
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public FFmpegProfileRepository(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

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