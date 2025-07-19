using ErsatzTV.Application.Tree;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class GetPlaylistTreeHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetPlaylistTree, TreeViewModel>
{
    public async Task<TreeViewModel> Handle(
        GetPlaylistTree request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<PlaylistGroup> playlistGroups = await dbContext.PlaylistGroups
            .AsNoTracking()
            .OrderByDescending(pg => pg.IsSystem)
            .ThenBy(pg => pg.Name)
            .Include(g => g.Playlists)
            .ToListAsync(cancellationToken);

        return Mapper.ProjectToViewModel(playlistGroups);
    }
}
