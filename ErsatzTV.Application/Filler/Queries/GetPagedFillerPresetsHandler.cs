using Dapper;
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
        int count = await dbContext.Connection.QuerySingleAsync<int>(@"SELECT COUNT (*) FROM FillerPreset");
        List<FillerPresetViewModel> page = await dbContext.FillerPresets.FromSqlRaw(
                @"SELECT * FROM FillerPreset
                    ORDER BY Name
                    COLLATE NOCASE
                    LIMIT {0} OFFSET {1}",
                request.PageSize,
                request.PageNum * request.PageSize)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedFillerPresetsViewModel(count, page);
    }
}
