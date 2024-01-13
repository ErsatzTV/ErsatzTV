using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetTemplateItemsHandler(IDbContextFactory<TvContext> dbContextFactory) : IRequestHandler<GetTemplateItems, List<TemplateItemViewModel>>
{
    public async Task<List<TemplateItemViewModel>> Handle(GetTemplateItems request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return [];

        // return await dbContext.TemplateItems
        //     .AsNoTracking()
        //     .Filter(i => i.BlockId == request.BlockId)
        //     .Include(i => i.Collection)
        //     .Include(i => i.MultiCollection)
        //     .Include(i => i.SmartCollection)
        //     .Include(i => i.MediaItem)
        //     .ThenInclude(i => (i as Season).SeasonMetadata)
        //     .ThenInclude(sm => sm.Artwork)
        //     .Include(i => i.MediaItem)
        //     .ThenInclude(i => (i as Season).Show)
        //     .ThenInclude(s => s.ShowMetadata)
        //     .ThenInclude(sm => sm.Artwork)
        //     .Include(i => i.MediaItem)
        //     .ThenInclude(i => (i as Show).ShowMetadata)
        //     .ThenInclude(sm => sm.Artwork)
        //     .Include(i => i.MediaItem)
        //     .ThenInclude(i => (i as Artist).ArtistMetadata)
        //     .ThenInclude(am => am.Artwork)
        //     .ToListAsync(cancellationToken)
        //     .Map(items => items.Map(Mapper.ProjectToViewModel).ToList());
    }
}
