using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class GetTraktListByIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetTraktListById, Option<TraktListViewModel>>
{
    public async Task<Option<TraktListViewModel>> Handle(GetTraktListById request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.TraktLists
            .Include(tl => tl.Items)
            .SelectOneAsync(tl => tl.Id, tl => tl.Id == request.Id)
            .MapT(Mapper.ProjectToViewModel);
    }
}
