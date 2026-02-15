using System.IO.Abstractions;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.FFmpeg;
using ErsatzTV.Infrastructure.Data;
using ErsatzTV.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Streaming;

public class GetSlugProcessByChannelNumberHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IFileSystem fileSystem,
    IFFmpegProcessService ffmpegProcessService,
    ILocalStatisticsProvider localStatisticsProvider)
    : FFmpegProcessHandler<GetSlugProcessByChannelNumber>(dbContextFactory)
{
    protected override async Task<Either<BaseError, PlayoutItemProcessModel>> GetProcess(
        TvContext dbContext,
        GetSlugProcessByChannelNumber request,
        Channel channel,
        string ffmpegPath,
        string ffprobePath,
        CancellationToken cancellationToken)
    {
        string videoPath = fileSystem.Path.Combine(FileSystemLayout.ResourcesCacheFolder, "slug.mp4");

        bool saveReports = await dbContext.ConfigElements
            .GetValue<bool>(ConfigElementKey.FFmpegSaveReports, cancellationToken)
            .Map(result => result.IfNone(false));

        Either<BaseError, MediaVersion> maybeVersion =
            await localStatisticsProvider.GetStatistics(ffprobePath, videoPath);
        foreach (var error in maybeVersion.LeftToSeq())
        {
            return error;
        }

        var version = maybeVersion.RightToSeq().Head();

        var mediaItem = new OtherVideo
        {
            MediaVersions = [version]
        };

        TimeSpan duration = version.Duration;
        foreach (double slugSeconds in request.SlugSeconds)
        {
            TimeSpan seconds = TimeSpan.FromSeconds(slugSeconds);
            if (seconds > TimeSpan.Zero && seconds < duration)
            {
                duration = seconds;
            }
        }

        DateTimeOffset finish = request.Now.Add(duration);

        PlayoutItemResult playoutItemResult = await ffmpegProcessService.ForPlayoutItem(
            ffmpegPath,
            ffprobePath,
            saveReports,
            channel,
            new MediaItemVideoVersion(mediaItem, version),
            new MediaItemAudioVersion(mediaItem, version),
            videoPath,
            videoPath,
            _ => Task.FromResult<List<Subtitle>>([]),
            string.Empty,
            string.Empty,
            string.Empty,
            ChannelSubtitleMode.None,
            request.Now,
            finish,
            request.Now,
            duration,
            [],
            [],
            channel.FFmpegProfile.VaapiDisplay,
            channel.FFmpegProfile.VaapiDriver,
            channel.FFmpegProfile.VaapiDevice,
            Optional(channel.FFmpegProfile.QsvExtraHardwareFrames),
            request.HlsRealtime,
            StreamInputKind.Vod,
            FillerKind.None,
            inPoint: TimeSpan.Zero,
            request.ChannelStartTime,
            request.PtsOffset,
            request.TargetFramerate,
            Option<string>.None,
            _ => { },
            canProxy: true,
            cancellationToken);

        var result = new PlayoutItemProcessModel(
            playoutItemResult.Process,
            playoutItemResult.GraphicsEngineContext,
            duration,
            finish,
            isComplete: true,
            request.Now.ToUnixTimeSeconds(),
            playoutItemResult.MediaItemId,
            Optional(channel.PlayoutOffset),
            !request.HlsRealtime);

        return Right<BaseError, PlayoutItemProcessModel>(result);
    }
}
