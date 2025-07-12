using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class GetPlaylistTreeHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPlaylistTree, PlaylistTreeViewModel>
{
    public async Task<PlaylistTreeViewModel> Handle(
        GetPlaylistTree request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<PlaylistGroup> playlistGroups = await dbContext.PlaylistGroups
            .AsNoTracking()
            .Include(g => g.Playlists)
            .ToListAsync(cancellationToken);

        return Mapper.ProjectToViewModel(playlistGroups);
    }
}
