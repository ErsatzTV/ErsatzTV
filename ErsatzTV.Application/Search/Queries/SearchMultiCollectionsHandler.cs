using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.Search;

public class SearchMultiCollectionsHandler : IRequestHandler<SearchMultiCollections, List<MultiCollectionViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public SearchMultiCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<MultiCollectionViewModel>> Handle(
        SearchMultiCollections request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.MultiCollections.FromSqlRaw(
                @"SELECT * FROM MultiCollection
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
