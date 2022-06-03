using ErsatzTV.Core.Api.FFmpegProfiles;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.FFmpegProfiles.Mapper;

namespace ErsatzTV.Application.FFmpegProfiles;

public class
    GetFFmpegProfileByIdForApiHandler : IRequestHandler<GetFFmpegFullProfileByIdForApi,
        Option<FFmpegFullProfileResponseModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetFFmpegProfileByIdForApiHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<Option<FFmpegFullProfileResponseModel>> Handle(
        GetFFmpegFullProfileByIdForApi request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.FFmpegProfiles
            .Include(p => p.Resolution)
            .SelectOneAsync(p => p.Id, p => p.Id == request.Id)
            .MapT(ProjectToFullResponseModel);
    }
}
