using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class GetPlaylistsByPlaylistGroupIdHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPlaylistsByPlaylistGroupId, List<PlaylistViewModel>>
{
    public async Task<List<PlaylistViewModel>> Handle(
        GetPlaylistsByPlaylistGroupId request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Playlists
            .AsNoTracking()
            .Filter(p => p.PlaylistGroupId == request.PlaylistGroupId)
            .ToListAsync(cancellationToken)
            .Map(items => items.Map(Mapper.ProjectToViewModel).ToList());
    }
}
