using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetDecoTemplateItemsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetDecoTemplateItems, List<DecoTemplateItemViewModel>>
{
    public async Task<List<DecoTemplateItemViewModel>> Handle(
        GetDecoTemplateItems request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        // drop items that are invalid
        return await dbContext.DecoTemplateItems
            .AsNoTracking()
            .Filter(i => i.DecoTemplateId == request.DecoTemplateId)
            .Include(i => i.Deco)
            .ToListAsync(cancellationToken)
            .Map(items => items
                .Map(Mapper.ProjectToViewModel)
                .Filter(i => i.StartTime < i.EndTime || i.EndTime.TimeOfDay == TimeSpan.Zero)
                .ToList());
    }
}
