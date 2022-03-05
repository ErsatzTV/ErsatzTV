using Dapper;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetPagedSmartCollectionsHandler : IRequestHandler<GetPagedSmartCollections, PagedSmartCollectionsViewModel>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetPagedSmartCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PagedSmartCollectionsViewModel> Handle(
        GetPagedSmartCollections request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        int count = await dbContext.Connection.QuerySingleAsync<int>(@"SELECT COUNT (*) FROM SmartCollection");
        List<SmartCollectionViewModel> page = await dbContext.SmartCollections.FromSqlRaw(
                @"SELECT * FROM SmartCollection
                    ORDER BY Name
                    COLLATE NOCASE
                    LIMIT {0} OFFSET {1}",
                request.PageSize,
                request.PageNum * request.PageSize)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedSmartCollectionsViewModel(count, page);
    }
}