using Dapper;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.MediaCollections;

public class GetPagedMultiCollectionsHandler : IRequestHandler<GetPagedMultiCollections, PagedMultiCollectionsViewModel>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetPagedMultiCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PagedMultiCollectionsViewModel> Handle(
        GetPagedMultiCollections request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        int count = await dbContext.Connection.QuerySingleAsync<int>(@"SELECT COUNT (*) FROM MultiCollection");
        List<MultiCollectionViewModel> page = await dbContext.MultiCollections.FromSqlRaw(
                @"SELECT * FROM MultiCollection
                    ORDER BY Name
                    COLLATE NOCASE
                    LIMIT {0} OFFSET {1}",
                request.PageSize,
                request.PageNum * request.PageSize)
            .Include(mc => mc.MultiCollectionItems)
            .ThenInclude(i => i.Collection)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());

        return new PagedMultiCollectionsViewModel(count, page);
    }
}