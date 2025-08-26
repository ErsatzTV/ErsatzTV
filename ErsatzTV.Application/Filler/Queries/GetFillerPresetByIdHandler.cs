using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Filler.Mapper;

namespace ErsatzTV.Application.Filler;

public class GetFillerPresetByIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetFillerPresetById, Option<FillerPresetViewModel>>
{
    public async Task<Option<FillerPresetViewModel>> Handle(
        GetFillerPresetById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.FillerPresets
            .AsNoTracking()
            .Include(i => i.Playlist)
            .SelectOneAsync(c => c.Id, c => c.Id == request.Id, cancellationToken)
            .MapT(ProjectToViewModel);
    }
}
