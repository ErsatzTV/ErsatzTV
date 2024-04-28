using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class DeletePlaylistGroupHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<DeletePlaylistGroup, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(DeletePlaylistGroup request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Option<PlaylistGroup> maybePlaylistGroup = await dbContext.PlaylistGroups
            .SelectOneAsync(p => p.Id, p => p.Id == request.PlaylistGroupId);

        foreach (PlaylistGroup playlistGroup in maybePlaylistGroup)
        {
            dbContext.PlaylistGroups.Remove(playlistGroup);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return maybePlaylistGroup.Match(
            _ => Option<BaseError>.None,
            () => BaseError.New($"PlaylistGroup {request.PlaylistGroupId} does not exist."));
    }
}
