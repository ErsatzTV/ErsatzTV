using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class UpdateDefaultDecoHandler(IDbContextFactory<TvContext> dbContextFactory) : IRequestHandler<UpdateDefaultDeco>
{
    public async Task Handle(UpdateDefaultDeco request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        await dbContext.Playouts
            .Where(p => p.Id == request.PlayoutId)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.DecoId, p => request.DecoId), cancellationToken);
    }
}
