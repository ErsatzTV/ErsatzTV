using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetTemplateItemsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetTemplateItems, List<TemplateItemViewModel>>
{
    public async Task<List<TemplateItemViewModel>> Handle(GetTemplateItems request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        // drop items that are invalid
        return await dbContext.TemplateItems
            .AsNoTracking()
            .Filter(i => i.TemplateId == request.TemplateId)
            .Include(i => i.Block)
            .ToListAsync(cancellationToken)
            .Map(
                items => items
                    .Map(Mapper.ProjectToViewModel)
                    .Filter(i => i.StartTime < i.EndTime || i.EndTime.TimeOfDay == TimeSpan.Zero)
                    .ToList());
    }
}
