using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Filler.Mapper;

namespace ErsatzTV.Application.Filler;

public class GetPagedFillerPresetsHandler : IRequestHandler<GetPagedFillerPresets, PagedFillerPresetsViewModel>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetPagedFillerPresetsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<PagedFillerPresetsViewModel> Handle(
        GetPagedFillerPresets request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        int count = await dbContext.FillerPresets.CountAsync(cancellationToken);
        List<FillerPresetViewModel> page = await dbContext.FillerPresets
            .AsNoTracking()
            .OrderBy(f => EF.Functions.Collate(f.Name, TvContext.CaseInsensitiveCollation))
            .Skip(request.PageNum * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedFillerPresetsViewModel(count, page);
    }
}
