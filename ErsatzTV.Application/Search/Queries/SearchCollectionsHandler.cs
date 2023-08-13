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
        return await dbContext.Collections
            .AsNoTracking()
            .Where(c => EF.Functions.Like(EF.Functions.Collate(c.Name, TvContext.CaseInsensitiveCollation), $"%{request.Query}%"))
            .OrderBy(c => EF.Functions.Collate(c.Name, TvContext.CaseInsensitiveCollation))
            .Take(10)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
