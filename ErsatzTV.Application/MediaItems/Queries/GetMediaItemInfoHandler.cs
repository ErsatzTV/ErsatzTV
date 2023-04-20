using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Extensions;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.MediaItems;

public class GetMediaItemInfoHandler : IRequestHandler<GetMediaItemInfo, Either<BaseError, MediaItemInfo>>
{
    private readonly IDbContextFactory<TvContext> _dbContextFactory;

    public GetMediaItemInfoHandler(IDbContextFactory<TvContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Either<BaseError, MediaItemInfo>> Handle(
        GetMediaItemInfo request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using TvContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            Option<MediaItemInfo> mediaItem = await dbContext.MediaItems
                .AsNoTracking()
                .Include(i => i.LibraryPath)
                .ThenInclude(lp => lp.Library)
                .ThenInclude(l => l.MediaSource)
                // TODO: support all media types here
                .Include(i => (i as Movie).MediaVersions)
                .ThenInclude(mv => mv.Streams)
                .Include(i => (i as Episode).MediaVersions)
                .ThenInclude(mv => mv.Streams)
                .SelectOneAsync(i => i.Id, i => i.Id == request.Id)
                .MapT(Project);

            return mediaItem.ToEither(BaseError.New("Unable to locate media item"));
        }
        catch (Exception ex)
        {
            return BaseError.New(ex.ToString());
        }
    }

    private static MediaItemInfo Project(MediaItem mediaItem)
    {
        MediaVersion version = mediaItem.GetHeadVersion();

        string serverName = mediaItem.LibraryPath.Library.MediaSource switch
        {
            PlexMediaSource plexMediaSource => plexMediaSource.ServerName,
            EmbyMediaSource embyMediaSource => embyMediaSource.ServerName,
            JellyfinMediaSource jellyfinMediaSource => jellyfinMediaSource.ServerName,
            _ => null
        };
        
        return new MediaItemInfo(
            mediaItem.Id,
            mediaItem.GetType().Name,
            mediaItem.LibraryPath.Library.GetType().Name,
            serverName,
            mediaItem.LibraryPath.Library.Name,
            mediaItem.State,
            version.Duration,
            version.SampleAspectRatio,
            version.DisplayAspectRatio,
            version.RFrameRate,
            version.VideoScanKind,
            version.Width,
            version.Height,
            version.Streams.OrderBy(s => s.Index).Map(Project).ToList());
    }

    private static MediaItemInfoStream Project(MediaStream mediaStream) =>
        new(
            mediaStream.Index,
            mediaStream.MediaStreamKind,
            mediaStream.Title,
            mediaStream.Codec,
            mediaStream.Profile,
            mediaStream.Language,
            mediaStream.Channels > 0 ? mediaStream.Channels : null,
            mediaStream.Default,
            mediaStream.Forced,
            mediaStream.AttachedPic,
            mediaStream.PixelFormat,
            mediaStream.ColorRange,
            mediaStream.ColorSpace,
            mediaStream.ColorTransfer,
            mediaStream.ColorPrimaries,
            mediaStream.BitsPerRawSample,
            mediaStream.FileName,
            mediaStream.MimeType);
}
