using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.FFmpegProfiles.Mapper;

namespace ErsatzTV.Application.FFmpegProfiles;

public class GetAllFFmpegProfilesHandler : IRequestHandler<GetAllFFmpegProfiles, List<FFmpegProfileViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetAllFFmpegProfilesHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<FFmpegProfileViewModel>> Handle(
        GetAllFFmpegProfiles request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.FFmpegProfiles
            .Include(p => p.Resolution)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
