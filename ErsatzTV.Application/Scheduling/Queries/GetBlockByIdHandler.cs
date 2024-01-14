using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetBlockByIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetBlockById, Option<BlockViewModel>>
{
    public async Task<Option<BlockViewModel>> Handle(GetBlockById request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Blocks
            .SelectOneAsync(b => b.Id, b => b.Id == request.BlockId)
            .MapT(Mapper.ProjectToViewModel);
    }
}
