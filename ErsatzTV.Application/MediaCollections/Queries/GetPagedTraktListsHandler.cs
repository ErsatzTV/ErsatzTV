using Dapper;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetPagedTraktListsHandler : IRequestHandler<GetPagedTraktLists, PagedTraktListsViewModel>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetPagedTraktListsHandler(IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PagedTraktListsViewModel> Handle(
        GetPagedTraktLists request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        int count = await dbContext.Connection.QuerySingleAsync<int>(@"SELECT COUNT (*) FROM TraktList");
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