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
            .ToListAsync(cancellationToken)
            .Map(items => items.Map(Mapper.ProjectToViewModel).ToList());
    }
}
