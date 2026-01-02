using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Filler.Mapper;

namespace ErsatzTV.Application.Filler;

public class GetPagedFillerPresetsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPagedFillerPresets, PagedFillerPresetsViewModel>
{
    public async Task<PagedFillerPresetsViewModel> Handle(
        GetPagedFillerPresets request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        int count = await dbContext.FillerPresets.CountAsync(cancellationToken);
        List<FillerPresetViewModel> page = await dbContext.FillerPresets
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .Skip(request.PageNum * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedFillerPresetsViewModel(count, page);
    }
}
