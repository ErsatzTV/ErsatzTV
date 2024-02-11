using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetBlockItemsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetBlockItems, List<BlockItemViewModel>>
{
    public async Task<List<BlockItemViewModel>> Handle(GetBlockItems request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<BlockItem> allItems = await dbContext.BlockItems
            .AsNoTracking()
            .Filter(i => i.BlockId == request.BlockId)
            .Include(i => i.Collection)
            .Include(i => i.MultiCollection)
            .Include(i => i.SmartCollection)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Season).SeasonMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Season).Show)
            .ThenInclude(s => s.ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Show).ShowMetadata)
            .ThenInclude(sm => sm.Artwork)
            .Include(i => i.MediaItem)
            .ThenInclude(i => (i as Artist).ArtistMetadata)
            .ThenInclude(am => am.Artwork)
            .ToListAsync(cancellationToken);

        if (allItems.All(bi => bi.IncludeInProgramGuide == false))
        {
            foreach (BlockItem bi in allItems)
            {
                bi.IncludeInProgramGuide = true;
            }
        }

        return allItems.Map(Mapper.ProjectToViewModel).ToList();
    }
}
