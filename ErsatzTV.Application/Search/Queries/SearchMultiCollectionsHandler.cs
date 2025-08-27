using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using static ErsatzTV.Application.MediaCollections.Mapper;

namespace ErsatzTV.Application.Search;

public class SearchMultiCollectionsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<SearchMultiCollections, List<MultiCollectionViewModel>>
{
    public async Task<List<MultiCollectionViewModel>> Handle(
        SearchMultiCollections request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.MultiCollections
            .AsNoTracking()
            .Where(c => EF.Functions.Like(
                EF.Functions.Collate(c.Name, TvContext.CaseInsensitiveCollation),
                $"%{request.Query}%"))
            .OrderBy(c => EF.Functions.Collate(c.Name, TvContext.CaseInsensitiveCollation))
            .Take(10)
            .ToListAsync(cancellationToken)
            .Map(list => list.Map(ProjectToViewModel).ToList());
    }
}
