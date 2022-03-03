using System.Data;
using Dapper;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetPagedSmartCollectionsHandler : IRequestHandler<GetPagedSmartCollections, PagedSmartCollectionsViewModel>
{
    private readonly IDbConnection _dbConnection;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetPagedSmartCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory, IDbConnection dbConnection)
    {
        _dbContextFactory = dbContextFactory;
        _dbConnection = dbConnection;
    }

    public async Task<PagedSmartCollectionsViewModel> Handle(
        GetPagedSmartCollections request,
        CancellationToken cancellationToken)
    {
        int count = await _dbConnection.QuerySingleAsync<int>(@"SELECT COUNT (*) FROM SmartCollection");

        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
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