using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class DeletePlaylistHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<DeletePlaylist, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(DeletePlaylist request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<Playlist> maybePlaylist = await dbContext.Playlists
            .SelectOneAsync(p => p.Id, p => p.Id == request.PlaylistId);

        foreach (Playlist playlist in maybePlaylist)
        {
            dbContext.Playlists.Remove(playlist);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return maybePlaylist.Match(
            _ => Option<BaseError>.None,
            () => BaseError.New($"Playlist {request.PlaylistId} does not exist."));
    }
}
