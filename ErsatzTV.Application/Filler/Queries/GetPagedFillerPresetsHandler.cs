using System.Data;
using Dapper;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.Filler.Mapper;

namespace ErsatzTV.Application.Filler;

public class GetPagedFillerPresetsHandler : IRequestHandler<GetPagedFillerPresets, PagedFillerPresetsViewModel>
{
    private readonly IDbConnection _dbConnection;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetPagedFillerPresetsHandler(IDbContextFactory<TvContext> dbContextFactory, IDbConnection dbConnection)
    {
        _dbContextFactory = dbContextFactory;
        _dbConnection = dbConnection;
    }

    public async Task<PagedFillerPresetsViewModel> Handle(
        GetPagedFillerPresets request,
        CancellationToken cancellationToken)
    {
        int count = await _dbConnection.QuerySingleAsync<int>(@"SELECT COUNT (*) FROM FillerPreset");

        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
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