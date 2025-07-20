using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaItems;

public class GetRemoteStreamByIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetRemoteStreamById, Option<RemoteStreamViewModel>>
{
    public async Task<Option<RemoteStreamViewModel>> Handle(
        GetRemoteStreamById request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.RemoteStreams
            .SelectOneAsync(rs => rs.Id, rs => rs.Id == request.RemoteStreamId)
            .MapT(Mapper.ProjectToViewModel);
    }
}
