using System.Data;
using Dapper;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetPagedCollectionsHandler : IRequestHandler<GetPagedCollections, PagedMediaCollectionsViewModel>
{
    private readonly IDbConnection _dbConnection;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetPagedCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory, IDbConnection dbConnection)
    {
        _dbContextFactory = dbContextFactory;
        _dbConnection = dbConnection;
    }

    public async Task<PagedMediaCollectionsViewModel> Handle(
        GetPagedCollections request,
        CancellationToken cancellationToken)
    {
        int count = await _dbConnection.QuerySingleAsync<int>(@"SELECT COUNT (*) FROM Collection");

        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        List<MediaCollectionViewModel> page = await dbContext.Collections.FromSqlRaw(
                @"SELECT * FROM Collection
                    ORDER BY Name
                    COLLATE NOCASE
                    LIMIT {0} OFFSET {1}",
                request.PageSize,
                request.PageNum * request.PageSize)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedMediaCollectionsViewModel(count, page);
    }
}