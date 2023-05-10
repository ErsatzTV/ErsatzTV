using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.Search;

public class SearchCollectionsHandler : IRequestHandler<SearchCollections, List<MediaCollectionViewModel>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public SearchCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public async Task<List<MediaCollectionViewModel>> Handle(
        SearchCollections request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Collections.FromSqlRaw(
                @"SELECT * FROM Collection
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
