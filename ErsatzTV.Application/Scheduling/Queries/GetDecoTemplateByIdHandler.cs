using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetDecoTemplateByIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetDecoTemplateById, Option<DecoTemplateViewModel>>
{
    public async Task<Option<DecoTemplateViewModel>> Handle(
        GetDecoTemplateById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.DecoTemplates
            .AsNoTracking()
            .Include(dt => dt.DecoTemplateGroup)
            .SelectOneAsync(b => b.Id, b => b.Id == request.DecoTemplateId)
            .MapT(Mapper.ProjectToViewModel);
    }
}
