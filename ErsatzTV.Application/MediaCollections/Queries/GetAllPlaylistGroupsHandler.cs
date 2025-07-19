using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class GetAllPlaylistGroupsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAllPlaylistGroups, List<PlaylistGroupViewModel>>
{
    public async Task<List<PlaylistGroupViewModel>> Handle(
        GetAllPlaylistGroups request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<PlaylistGroup> playlistGroups = await dbContext.PlaylistGroups
            .AsNoTracking()
            .OrderByDescending(pg => pg.IsSystem)
            .ThenBy(pg => pg.Name)
            .Include(g => g.Playlists)
            .ToListAsync(cancellationToken);

        return playlistGroups.Map(Mapper.ProjectToViewModel).ToList();
    }
}
