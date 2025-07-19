using ErsatzTV.Core;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class UpdateTraktListHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<UpdateTraktList, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(UpdateTraktList request, CancellationToken cancellationToken)
    {
        try
        {
            await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            await dbContext.TraktLists
                .Where(tl => tl.Id == request.Id)
                .ExecuteUpdateAsync(
                    u => u.SetProperty(p => p.AutoRefresh, p => request.AutoRefresh),
                    cancellationToken);

            return Option<BaseError>.None;
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }
}
