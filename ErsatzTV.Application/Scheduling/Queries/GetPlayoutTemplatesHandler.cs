using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetPlayoutTemplatesHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPlayoutTemplates, List<PlayoutTemplateViewModel>>
{
    public async Task<List<PlayoutTemplateViewModel>> Handle(
        GetPlayoutTemplates request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<PlayoutTemplate> playoutTemplates = await dbContext.PlayoutTemplates
            .AsNoTracking()
            .Filter(t => t.PlayoutId == request.PlayoutId)
            .Include(t => t.Template)
            .ToListAsync(cancellationToken);

        return playoutTemplates.Map(Mapper.ProjectToViewModel).ToList();
    }
}
