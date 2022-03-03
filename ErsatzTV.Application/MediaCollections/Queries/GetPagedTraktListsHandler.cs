using System.Data;
using Dapper;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetPagedTraktListsHandler : IRequestHandler<GetPagedTraktLists, PagedTraktListsViewModel>
{
    private readonly IDbConnection _dbConnection;
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetPagedTraktListsHandler(IDbContextFactory<TvContext> dbContextFactory, IDbConnection dbConnection)
    {
        _dbContextFactory = dbContextFactory;
        _dbConnection = dbConnection;
    }

    public async Task<PagedTraktListsViewModel> Handle(
        GetPagedTraktLists request,
        CancellationToken cancellationToken)
    {
        int count = await _dbConnection.QuerySingleAsync<int>(@"SELECT COUNT (*) FROM TraktList");

        await using TvContext dbContext = _dbContextFactory.CreateDbContext();
        List<TraktListViewModel> page = await dbContext.TraktLists.FromSqlRaw(
                @"SELECT * FROM TraktList
                    ORDER BY Name
                    COLLATE NOCASE
                    LIMIT {0} OFFSET {1}",
                request.PageSize,
                request.PageNum * request.PageSize)
            .Include(l => l.Items)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedTraktListsViewModel(count, page);
    }
}