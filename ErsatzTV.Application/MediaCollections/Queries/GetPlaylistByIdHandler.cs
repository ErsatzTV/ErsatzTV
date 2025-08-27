using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class GetPlaylistByIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPlaylistById, Option<PlaylistViewModel>>
{
    public async Task<Option<PlaylistViewModel>> Handle(GetPlaylistById request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Playlists
            .SelectOneAsync(b => b.Id, b => b.Id == request.PlaylistId, cancellationToken)
            .MapT(Mapper.ProjectToViewModel);
    }
}
