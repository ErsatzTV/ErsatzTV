using ErsatzTV.Core.Api.FFmpegProfiles;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.FFmpegProfiles.Mapper;

namespace ErsatzTV.Application.FFmpegProfiles;

public class
    GetAllFFmpegProfilesForApiHandler : IRequestHandler<GetAllFFmpegProfilesForApi, List<FFmpegProfileResponseModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetAllFFmpegProfilesForApiHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<FFmpegProfileResponseModel>> Handle(
        GetAllFFmpegProfilesForApi request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        List<FFmpegProfile> ffmpegProfiles = await dbContext.FFmpegProfiles
            .AsNoTracking()
            .Include(p => p.Resolution)
            .ToListAsync(cancellationToken);
        return ffmpegProfiles.Map(ProjectToResponseModel).ToList();
    }
}
