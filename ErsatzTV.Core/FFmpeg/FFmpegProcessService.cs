using System.Diagnostics;
using Bugsnag;
using CliWrap;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.FFmpeg;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Logging;
using MediaStream = ErsatzTV.Core.Domain.MediaStream;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegProcessService
{
    private readonly IClient _client;
    private readonly IFFmpegStreamSelector _ffmpegStreamSelector;
    private readonly ILogger<FFmpegProcessService> _logger;
    private readonly ITempFilePool _tempFilePool;

    public FFmpegProcessService(
        IFFmpegStreamSelector ffmpegStreamSelector,
        ITempFilePool tempFilePool,
        IClient client,
        ILogger<FFmpegProcessService> logger)
    {
        _ffmpegStreamSelector = ffmpegStreamSelector;
        _tempFilePool = tempFilePool;
        _client = client;
        _logger = logger;
    }

    public async Task<Either<BaseError, string>> GenerateSongImage(
        string ffmpegPath,
        string ffprobePath,
        Option<string> subtitleFile,
        Channel channel,
        MediaVersion videoVersion,
        string videoPath,
        bool boxBlur,
        Option<string> watermarkPath,
        WatermarkLocation watermarkLocation,
        int horizontalMarginPercent,
        int verticalMarginPercent,
        int watermarkWidthPercent,
        CancellationToken cancellationToken)
    {
        try
        {
            string outputFile = _tempFilePool.GetNextTempFile(TempFileCategory.SongBackground);

            MediaStream videoStream = await _ffmpegStreamSelector.SelectVideoStream(videoVersion);

            Option<WatermarkOptions> watermarkOptions = Option<WatermarkOptions>.None;
            if (videoVersion is FallbackMediaVersion or CoverArtMediaVersion)
            {
                var songWatermark = new ChannelWatermark
                {
                    Mode = ChannelWatermarkMode.Permanent,
                    HorizontalMarginPercent = horizontalMarginPercent,
                    VerticalMarginPercent = verticalMarginPercent,
                    Location = watermarkLocation,
                    Size = WatermarkSize.Scaled,
                    WidthPercent = watermarkWidthPercent,
                    Opacity = 100
                };

                watermarkOptions = new WatermarkOptions(
                    songWatermark,
                    await watermarkPath.IfNoneAsync(videoVersion.MediaFiles.Head().Path),
                    0);
            }

            FFmpegPlaybackSettings playbackSettings =
                FFmpegPlaybackSettingsCalculator.CalculateErrorSettings(
                    StreamingMode.TransportStream,
                    channel.FFmpegProfile,
                    false);

            FFmpegPlaybackSettings scalePlaybackSettings = FFmpegPlaybackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                channel.FFmpegProfile,
                videoVersion,
                videoStream,
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch,
                TimeSpan.Zero,
                false,
                StreamInputKind.Vod,
                Option<FrameRate>.None);

            scalePlaybackSettings.AudioChannels = Option<int>.None;

            FFmpegProcessBuilder builder = new FFmpegProcessBuilder(ffmpegPath)
                .WithThreads(1)
                .WithQuiet()
                .WithFormatFlags(playbackSettings.FormatFlags)
                .WithSongInput(videoPath, videoStream.PixelFormat, boxBlur)
                .WithWatermark(watermarkOptions, None, channel.FFmpegProfile.Resolution)
                .WithSubtitleFile(subtitleFile);

            foreach (IDisplaySize scaledSize in scalePlaybackSettings.ScaledSize)
            {
                builder = builder.WithScaling(scaledSize);

                if (NeedToPad(channel.FFmpegProfile.Resolution, scaledSize))
                {
                    builder = builder.WithBlackBars(channel.FFmpegProfile.Resolution);
                }
            }

            using Process process = builder
                .WithFilterComplex(
                    videoStream,
                    None,
                    videoPath,
                    None)
                .WithOutputFormat("apng", outputFile, "-pix_fmt", "rgb24")
                .Build();

            _logger.LogInformation(
                "ffmpeg song arguments {FFmpegArguments}",
                string.Join(" ", process.StartInfo.ArgumentList));

            await Cli.Wrap(process.StartInfo.FileName)
                .WithArguments(process.StartInfo.ArgumentList)
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync(cancellationToken);

            return outputFile;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generating song image");
            _client.Notify(ex);
            return Left(BaseError.New(ex.Message));
        }
    }

    private static bool NeedToPad(Resolution target, IDisplaySize displaySize) =>
        displaySize.Width != target.Width || displaySize.Height != target.Height;
}
