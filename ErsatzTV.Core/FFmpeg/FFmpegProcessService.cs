using System.Diagnostics;
using System.Text;
using Bugsnag;
using CliWrap;
using CliWrap.Buffered;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.FFmpeg.State;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegProcessService
{
    private readonly IClient _client;
    private readonly IFFmpegStreamSelector _ffmpegStreamSelector;
    private readonly IImageCache _imageCache;
    private readonly ILogger<FFmpegProcessService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly FFmpegPlaybackSettingsCalculator _playbackSettingsCalculator;
    private readonly ITempFilePool _tempFilePool;

    public FFmpegProcessService(
        FFmpegPlaybackSettingsCalculator ffmpegPlaybackSettingsService,
        IFFmpegStreamSelector ffmpegStreamSelector,
        IImageCache imageCache,
        ITempFilePool tempFilePool,
        IClient client,
        IMemoryCache memoryCache,
        ILogger<FFmpegProcessService> logger)
    {
        _playbackSettingsCalculator = ffmpegPlaybackSettingsService;
        _ffmpegStreamSelector = ffmpegStreamSelector;
        _imageCache = imageCache;
        _tempFilePool = tempFilePool;
        _client = client;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Command WrapSegmenter(string ffmpegPath, bool saveReports, Channel channel, string scheme, string host)
    {
        FFmpegPlaybackSettings playbackSettings = _playbackSettingsCalculator.ConcatSettings;

        Process process = new FFmpegProcessBuilder(ffmpegPath, saveReports, _logger)
            .WithThreads(1)
            .WithQuiet()
            .WithFormatFlags(playbackSettings.FormatFlags)
            .WithRealtimeOutput(true)
            .WithInput($"http://localhost:{Settings.ListenPort}/iptv/channel/{channel.Number}.m3u8?mode=segmenter")
            .WithMap("0")
            .WithCopyCodec()
            .WithMetadata(channel, None)
            .WithFormat("mpegts")
            .WithPipe()
            .Build();

        return Cli.Wrap(process.StartInfo.FileName)
            .WithArguments(process.StartInfo.ArgumentList)
            .WithValidation(CommandResultValidation.None)
            .WithEnvironmentVariables(process.StartInfo.Environment.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
            .WithStandardErrorPipe(PipeTarget.ToStream(Stream.Null));
    }

    public Command ConvertToPng(string ffmpegPath, string inputFile, string outputFile)
    {
        Process process = new FFmpegProcessBuilder(ffmpegPath, false, _logger)
            .WithThreads(1)
            .WithQuiet()
            .WithInput(inputFile)
            .WithOutputFormat("apng", outputFile)
            .Build();

        return Cli.Wrap(process.StartInfo.FileName)
            .WithArguments(process.StartInfo.ArgumentList)
            .WithValidation(CommandResultValidation.None)
            .WithEnvironmentVariables(process.StartInfo.Environment.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
            .WithStandardErrorPipe(PipeTarget.ToStream(Stream.Null));
    }

    public Command ExtractAttachedPicAsPng(string ffmpegPath, string inputFile, int streamIndex, string outputFile)
    {
        Process process = new FFmpegProcessBuilder(ffmpegPath, false, _logger)
            .WithThreads(1)
            .WithQuiet()
            .WithInput(inputFile)
            .WithMap($"0:{streamIndex}")
            .WithOutputFormat("apng", outputFile)
            .Build();

        return Cli.Wrap(process.StartInfo.FileName)
            .WithArguments(process.StartInfo.ArgumentList)
            .WithValidation(CommandResultValidation.None)
            .WithEnvironmentVariables(process.StartInfo.Environment.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
            .WithStandardErrorPipe(PipeTarget.ToStream(Stream.Null));
    }

    public async Task<Either<BaseError, string>> GenerateSongImage(
        string ffmpegPath,
        string ffprobePath,
        Option<string> subtitleFile,
        Channel channel,
        Option<ChannelWatermark> playoutItemWatermark,
        Option<ChannelWatermark> globalWatermark,
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

            Option<ChannelWatermark> watermarkOverride =
                videoVersion is FallbackMediaVersion or CoverArtMediaVersion
                    ? new ChannelWatermark
                    {
                        Mode = ChannelWatermarkMode.Permanent,
                        HorizontalMarginPercent = horizontalMarginPercent,
                        VerticalMarginPercent = verticalMarginPercent,
                        Location = watermarkLocation,
                        Size = WatermarkSize.Scaled,
                        WidthPercent = watermarkWidthPercent,
                        Opacity = 100
                    }
                    : None;

            Option<WatermarkOptions> watermarkOptions =
                await GetWatermarkOptions(
                    ffprobePath,
                    channel,
                    playoutItemWatermark,
                    globalWatermark,
                    videoVersion,
                    watermarkOverride,
                    watermarkPath);

            FFmpegPlaybackSettings playbackSettings =
                _playbackSettingsCalculator.CalculateErrorSettings(
                    StreamingMode.TransportStream,
                    channel.FFmpegProfile,
                    false);

            FFmpegPlaybackSettings scalePlaybackSettings = _playbackSettingsCalculator.CalculateSettings(
                StreamingMode.TransportStream,
                channel.FFmpegProfile,
                videoVersion,
                videoStream,
                None,
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch,
                TimeSpan.Zero,
                TimeSpan.Zero,
                false,
                Option<int>.None);

            FFmpegProcessBuilder builder = new FFmpegProcessBuilder(ffmpegPath, false, _logger)
                .WithThreads(1)
                .WithQuiet()
                .WithFormatFlags(playbackSettings.FormatFlags)
                .WithSongInput(videoPath, videoStream.Codec, videoStream.PixelFormat, boxBlur)
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
                    None,
                    playbackSettings.VideoFormat)
                .WithOutputFormat("apng", outputFile)
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

    private bool NeedToPad(IDisplaySize target, IDisplaySize displaySize) =>
        displaySize.Width != target.Width || displaySize.Height != target.Height;

    internal async Task<WatermarkOptions> GetWatermarkOptions(
        string ffprobePath,
        Channel channel,
        Option<ChannelWatermark> playoutItemWatermark,
        Option<ChannelWatermark> globalWatermark,
        MediaVersion videoVersion,
        Option<ChannelWatermark> watermarkOverride,
        Option<string> watermarkPath)
    {
        if (channel.StreamingMode != StreamingMode.HttpLiveStreamingDirect)
        {
            if (videoVersion is CoverArtMediaVersion)
            {
                return new WatermarkOptions(
                    watermarkOverride,
                    await watermarkPath.IfNoneAsync(videoVersion.MediaFiles.Head().Path),
                    0,
                    false);
            }

            // check for playout item watermark
            foreach (ChannelWatermark watermark in playoutItemWatermark)
            {
                switch (watermark.ImageSource)
                {
                    case ChannelWatermarkImageSource.Custom:
                        string customPath = _imageCache.GetPathForImage(
                            watermark.Image,
                            ArtworkKind.Watermark,
                            Option<int>.None);
                        return new WatermarkOptions(
                            await watermarkOverride.IfNoneAsync(watermark),
                            customPath,
                            None,
                            await IsAnimated(ffprobePath, customPath));
                    case ChannelWatermarkImageSource.ChannelLogo:
                        Option<string> maybeChannelPath = channel.Artwork
                            .Filter(a => a.ArtworkKind == ArtworkKind.Logo)
                            .HeadOrNone()
                            .Map(a => _imageCache.GetPathForImage(a.Path, ArtworkKind.Logo, Option<int>.None));
                        return new WatermarkOptions(
                            await watermarkOverride.IfNoneAsync(watermark),
                            maybeChannelPath,
                            None,
                            await maybeChannelPath.Match(
                                p => IsAnimated(ffprobePath, p),
                                () => Task.FromResult(false)));
                    default:
                        throw new NotSupportedException("Unsupported watermark image source");
                }
            }

            // check for channel watermark
            if (channel.Watermark != null)
            {
                switch (channel.Watermark.ImageSource)
                {
                    case ChannelWatermarkImageSource.Custom:
                        string customPath = _imageCache.GetPathForImage(
                            channel.Watermark.Image,
                            ArtworkKind.Watermark,
                            Option<int>.None);
                        return new WatermarkOptions(
                            await watermarkOverride.IfNoneAsync(channel.Watermark),
                            customPath,
                            None,
                            await IsAnimated(ffprobePath, customPath));
                    case ChannelWatermarkImageSource.ChannelLogo:
                        Option<string> maybeChannelPath = channel.Artwork
                            .Filter(a => a.ArtworkKind == ArtworkKind.Logo)
                            .HeadOrNone()
                            .Map(a => _imageCache.GetPathForImage(a.Path, ArtworkKind.Logo, Option<int>.None));
                        return new WatermarkOptions(
                            await watermarkOverride.IfNoneAsync(channel.Watermark),
                            maybeChannelPath,
                            None,
                            await maybeChannelPath.Match(
                                p => IsAnimated(ffprobePath, p),
                                () => Task.FromResult(false)));
                    default:
                        throw new NotSupportedException("Unsupported watermark image source");
                }
            }

            // check for global watermark
            foreach (ChannelWatermark watermark in globalWatermark)
            {
                switch (watermark.ImageSource)
                {
                    case ChannelWatermarkImageSource.Custom:
                        string customPath = _imageCache.GetPathForImage(
                            watermark.Image,
                            ArtworkKind.Watermark,
                            Option<int>.None);
                        return new WatermarkOptions(
                            await watermarkOverride.IfNoneAsync(watermark),
                            customPath,
                            None,
                            await IsAnimated(ffprobePath, customPath));
                    case ChannelWatermarkImageSource.ChannelLogo:
                        Option<string> maybeChannelPath = channel.Artwork
                            .Filter(a => a.ArtworkKind == ArtworkKind.Logo)
                            .HeadOrNone()
                            .Map(a => _imageCache.GetPathForImage(a.Path, ArtworkKind.Logo, Option<int>.None));
                        return new WatermarkOptions(
                            await watermarkOverride.IfNoneAsync(watermark),
                            maybeChannelPath,
                            None,
                            await maybeChannelPath.Match(
                                p => IsAnimated(ffprobePath, p),
                                () => Task.FromResult(false)));
                    default:
                        throw new NotSupportedException("Unsupported watermark image source");
                }
            }
        }

        return new WatermarkOptions(None, None, None, false);
    }

    private async Task<bool> IsAnimated(string ffprobePath, string path)
    {
        try
        {
            var cacheKey = $"image.animated.{Path.GetFileName(path)}";
            if (_memoryCache.TryGetValue(cacheKey, out bool animated))
            {
                return animated;
            }

            BufferedCommandResult result = await Cli.Wrap(ffprobePath)
                .WithArguments(
                    new[]
                    {
                        "-loglevel", "error",
                        "-select_streams", "v:0",
                        "-count_frames",
                        "-show_entries", "stream=nb_read_frames",
                        "-print_format", "csv",
                        path
                    })
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(Encoding.UTF8);

            if (result.ExitCode == 0)
            {
                string output = result.StandardOutput;
                output = output.Replace("stream,", string.Empty);
                if (int.TryParse(output, out int frameCount))
                {
                    bool isAnimated = frameCount > 1;
                    _memoryCache.Set(cacheKey, isAnimated, TimeSpan.FromDays(1));
                    return isAnimated;
                }
            }
            else
            {
                _logger.LogWarning(
                    "Error checking frame count for file {File}l exit code {ExitCode}",
                    path,
                    result.ExitCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking frame count for file {File}", path);
        }

        return false;
    }
}
