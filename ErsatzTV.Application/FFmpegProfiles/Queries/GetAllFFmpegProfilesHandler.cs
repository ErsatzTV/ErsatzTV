using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.FFmpegProfiles.Mapper;

namespace ErsatzTV.Application.FFmpegProfiles;

public class GetAllFFmpegProfilesHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAllFFmpegProfiles, List<FFmpegProfileViewModel>>
{
    public async Task<List<FFmpegProfileViewModel>> Handle(
        GetAllFFmpegProfiles request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.FFmpegProfiles
            .Include(p => p.Resolution)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
