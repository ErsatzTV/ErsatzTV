using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetTemplateByIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetTemplateById, Option<TemplateViewModel>>
{
    public async Task<Option<TemplateViewModel>> Handle(GetTemplateById request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Templates
            .SelectOneAsync(b => b.Id, b => b.Id == request.TemplateId)
            .MapT(Mapper.ProjectToViewModel);
    }
}
