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

    public async Task<List<SmartCollectionViewModel>> Handle(
        SearchSmartCollections request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.SmartCollections
            .AsNoTracking()
            .Where(c => EF.Functions.Like(EF.Functions.Collate(c.Name, TvContext.CaseInsensitiveCollation), $"%{request.Query}%"))
            .OrderBy(c => EF.Functions.Collate(c.Name, TvContext.CaseInsensitiveCollation))
            .Take(10)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
