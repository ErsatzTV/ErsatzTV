using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetDecoByIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetDecoById, Option<DecoViewModel>>
{
    public async Task<Option<DecoViewModel>> Handle(GetDecoById request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Decos
            .SelectOneAsync(b => b.Id, b => b.Id == request.DecoId)
            .MapT(Mapper.ProjectToViewModel);
    }
}
