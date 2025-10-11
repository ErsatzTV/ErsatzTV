using ErsatzTV.Core.Domain;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ErsatzTV.Application.Troubleshooting.Queries;

public class DecodePlayoutHistoryHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<DecodePlayoutHistory, PlayoutHistoryDetailsViewModel>
{
    public async Task<PlayoutHistoryDetailsViewModel> Handle(
        DecodePlayoutHistory request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var decodedKey = JsonConvert.DeserializeObject<BlockItemHistoryKey>(request.PlayoutHistory.Key);

        PlaybackOrder playbackOrder = decodedKey.PlaybackOrder ?? PlaybackOrder.None;
        CollectionType collectionType = decodedKey.CollectionType ?? CollectionType.Collection;

        string name = string.Empty;

        switch (collectionType)
        {
            case CollectionType.Collection:
                name = await dbContext.Collections
                    .AsNoTracking()
                    .Where(c => c.Id == (decodedKey.CollectionId ?? 0))
                    .Map(c => c.Name)
                    .FirstOrDefaultAsync(cancellationToken);
                break;
            case CollectionType.SmartCollection:
                name = await dbContext.SmartCollections
                    .AsNoTracking()
                    .Where(c => c.Id == (decodedKey.SmartCollectionId ?? 0))
                    .Map(c => c.Name)
                    .FirstOrDefaultAsync(cancellationToken);
                break;
        }

        string mediaItemType = string.Empty;
        string mediaItemTitle = string.Empty;

        Details details = JsonConvert.DeserializeObject<Details>(request.PlayoutHistory.Details);
        if (details?.MediaItemId != null)
        {
            Option<MediaItem> maybeMediaItem = await dbContext.MediaItems
                .AsNoTracking()
                .Include(i => i.LibraryPath)
                .ThenInclude(lp => lp.Library)
                .ThenInclude(l => l.MediaSource)
                .Include(i => (i as Movie).MovieMetadata)
                .Include(i => (i as Episode).EpisodeMetadata)
                .Include(i => (i as Episode).Season)
                .ThenInclude(s => s.Show)
                .ThenInclude(s => s.ShowMetadata)
                .Include(i => (i as OtherVideo).OtherVideoMetadata)
                .Include(i => (i as Image).ImageMetadata)
                .Include(i => (i as RemoteStream).RemoteStreamMetadata)
                .Include(i => (i as Song).SongMetadata)
                .Include(i => (i as MusicVideo).MusicVideoMetadata)
                .Include(i => (i as MusicVideo).Artist)
                .ThenInclude(a => a.ArtistMetadata)
                .SelectOneAsync(i => i.Id, i => i.Id == details.MediaItemId, cancellationToken);

            foreach (var mediaItem in maybeMediaItem)
            {
                mediaItemType = mediaItem switch
                {
                    Episode => "Episode",
                    Movie => "Movie",
                    MusicVideo => "Music Video",
                    OtherVideo => "Other Video",
                    Song => "Song",
                    Image => "Image",
                    RemoteStream => "Remote Stream",
                    _ => $"Unknown ({mediaItem.GetType().Name})"
                };

                mediaItemTitle = Playouts.Mapper.GetDisplayTitle(mediaItem, Option<string>.None);
            }
        }

        return new PlayoutHistoryDetailsViewModel(playbackOrder, collectionType, name, mediaItemType, mediaItemTitle);
    }

    private sealed record BlockItemHistoryKey(
        int? BlockId,
        PlaybackOrder? PlaybackOrder,
        CollectionType? CollectionType,
        int? CollectionId,
        int? SmartCollectionId);

    private sealed record Details(int? MediaItemId);
}
