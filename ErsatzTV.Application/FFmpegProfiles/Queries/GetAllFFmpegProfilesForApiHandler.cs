using ErsatzTV.Core.Api.FFmpegProfiles;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.FFmpegProfiles.Mapper;

namespace ErsatzTV.Application.FFmpegProfiles;

public class GetAllFFmpegProfilesForApiHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAllFFmpegProfilesForApi, List<FFmpegFullProfileResponseModel>>
{
    public async Task<List<FFmpegFullProfileResponseModel>> Handle(
        GetAllFFmpegProfilesForApi request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        List<FFmpegProfile> ffmpegProfiles = await dbContext.FFmpegProfiles
            .AsNoTracking()
            .Include(p => p.Resolution)
            .ToListAsync(cancellationToken);
        return ffmpegProfiles.Map(ProjectToFullResponseModel).ToList();
    }
}
