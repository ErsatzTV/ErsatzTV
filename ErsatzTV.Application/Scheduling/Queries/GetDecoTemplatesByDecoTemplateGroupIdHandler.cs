using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetDecoTemplatesByDecoTemplateGroupIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetDecoTemplatesByDecoTemplateGroupId, List<DecoTemplateViewModel>>
{
    public async Task<List<DecoTemplateViewModel>> Handle(
        GetDecoTemplatesByDecoTemplateGroupId request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.DecoTemplates
            .AsNoTracking()
            .Filter(i => i.DecoTemplateGroupId == request.DecoTemplateGroupId)
            .Include(dt => dt.DecoTemplateGroup)
            .ToListAsync(cancellationToken)
            .Map(items => items.OrderBy(dt => dt.Name).Map(Mapper.ProjectToViewModel).ToList());
    }
}
