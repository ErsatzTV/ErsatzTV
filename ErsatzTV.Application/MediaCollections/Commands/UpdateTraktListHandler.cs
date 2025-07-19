using System.Threading.Channels;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaCollections;

public class UpdateTraktListHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IEntityLocker entityLocker,
    ChannelWriter<IBackgroundServiceRequest> workerChannel)
    : IRequestHandler<UpdateTraktList, Option<BaseError>>
{
    public async Task<Option<BaseError>> Handle(UpdateTraktList request, CancellationToken cancellationToken)
    {
        try
        {
            await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            Option<TraktList> maybeTraktList = await dbContext.TraktLists
                .Include(tl => tl.Playlist)
                .SelectOneAsync(t => t.Id, t => t.Id == request.Id);

            foreach (TraktList traktList in maybeTraktList)
            {
                traktList.AutoRefresh = request.AutoRefresh;
                traktList.GeneratePlaylist = request.GeneratePlaylist;

                await dbContext.SaveChangesAsync(cancellationToken);

                if (request.GeneratePlaylist)
                {
                    if (traktList.PlaylistId is null)
                    {
                        PlaylistGroup traktListGroup = await dbContext.PlaylistGroups
                            .Filter(pg => pg.IsSystem)
                            .FirstOrDefaultAsync(cancellationToken);

                        var playlist = new Playlist
                        {
                            IsSystem = true,
                            Name = $"{traktList.User}/{traktList.List}",
                            PlaylistGroupId = traktListGroup.Id,
                            PlaylistGroup = traktListGroup
                        };

                        traktList.Playlist = playlist;

                        await dbContext.Playlists.AddAsync(playlist, cancellationToken);
                        await dbContext.SaveChangesAsync(cancellationToken);
                    }

                    if (entityLocker.LockTrakt())
                    {
                        await workerChannel.WriteAsync(new MatchTraktListItems(traktList.Id), cancellationToken);
                    }
                }
                else if (traktList.PlaylistId is not null)
                {
                    // delete playlist
                    await dbContext.Playlists
                        .Where(p => p.Id == traktList.PlaylistId)
                        .ExecuteDeleteAsync(cancellationToken);
                }
            }

            return Option<BaseError>.None;
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }
}
