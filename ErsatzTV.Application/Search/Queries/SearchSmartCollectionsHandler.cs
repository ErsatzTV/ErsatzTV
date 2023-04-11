using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.Search;

public class SearchSmartCollectionsHandler : IRequestHandler<SearchSmartCollections, List<SmartCollectionViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public SearchSmartCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<SmartCollectionViewModel>> Handle(SearchSmartCollections request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.SmartCollections.FromSqlRaw(
                @"SELECT * FROM SmartCollection
                    WHERE Name LIKE {0} 
                    ORDER BY Name
                    LIMIT 10
                    COLLATE NOCASE",
                $"%{request.Query}%")
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
