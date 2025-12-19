using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.FFmpegProfiles.Mapper;

namespace ErsatzTV.Application.FFmpegProfiles;

public class GetFFmpegProfileByIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetFFmpegProfileById, Option<FFmpegProfileViewModel>>
{
    public async Task<Option<FFmpegProfileViewModel>> Handle(
        GetFFmpegProfileById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.FFmpegProfiles
            .Include(p => p.Resolution)
            .SelectOneAsync(p => p.Id, p => p.Id == request.Id, cancellationToken)
            .MapT(ProjectToViewModel);
    }
}
