using ErsatzTV.Core;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class UpdateDefaultDecoHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<UpdateDefaultDeco, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(UpdateDefaultDeco request, CancellationToken cancellationToken)
    {
        try
        {
            await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            await dbContext.Playouts
                .Where(p => p.Id == request.PlayoutId)
                .ExecuteUpdateAsync(u => u.SetProperty(p => p.DecoId, p => request.DecoId), cancellationToken);

            return Option<BaseError>.None;
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }
}
