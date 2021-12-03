using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.FFmpeg
{
    public class FFmpegProcessService : IFFmpegProcessService
    {
        private readonly IFFmpegStreamSelector _ffmpegStreamSelector;
        private readonly IImageCache _imageCache;
        private readonly ITempFilePool _tempFilePool;
        private readonly ILogger<FFmpegProcessService> _logger;
        private readonly FFmpegPlaybackSettingsCalculator _playbackSettingsCalculator;

        public FFmpegProcessService(
            FFmpegPlaybackSettingsCalculator ffmpegPlaybackSettingsService,
            IFFmpegStreamSelector ffmpegStreamSelector,
            IImageCache imageCache,
            ITempFilePool tempFilePool,
            ILogger<FFmpegProcessService> logger)
        {
            _playbackSettingsCalculator = ffmpegPlaybackSettingsService;
            _ffmpegStreamSelector = ffmpegStreamSelector;
            _imageCache = imageCache;
            _tempFilePool = tempFilePool;
            _logger = logger;
        }

        public async Task<Process> ForPlayoutItem(
            string ffmpegPath,
            bool saveReports,
            Channel channel,
            MediaVersion videoVersion,
            MediaVersion audioVersion,
            string videoPath,
            string audioPath,
            DateTimeOffset start,
            DateTimeOffset finish,
            DateTimeOffset now,
            Option<ChannelWatermark> globalWatermark,
            VaapiDriver vaapiDriver,
            string vaapiDevice,
            bool hlsRealtime,
            FillerKind fillerKind,
            TimeSpan inPoint,
            TimeSpan outPoint)
        {
            MediaStream videoStream = await _ffmpegStreamSelector.SelectVideoStream(channel, videoVersion);
            Option<MediaStream> maybeAudioStream = await _ffmpegStreamSelector.SelectAudioStream(channel, audioVersion);

            FFmpegPlaybackSettings playbackSettings = _playbackSettingsCalculator.CalculateSettings(
                channel.StreamingMode,
                channel.FFmpegProfile,
                videoVersion,
                videoStream,
                maybeAudioStream,
                start,
                now,
                inPoint,
                outPoint);

            Option<WatermarkOptions> watermarkOptions =
                await GetWatermarkOptions(channel, globalWatermark, videoVersion, None, None);

            FFmpegProcessBuilder builder = new FFmpegProcessBuilder(ffmpegPath, saveReports, _logger)
                .WithThreads(playbackSettings.ThreadCount)
                .WithVaapiDriver(vaapiDriver, vaapiDevice)
                .WithHardwareAcceleration(
                    playbackSettings.HardwareAcceleration,
                    videoStream.PixelFormat,
                    playbackSettings.VideoCodec)
                .WithQuiet()
                .WithFormatFlags(playbackSettings.FormatFlags)
                .WithRealtimeOutput(playbackSettings.RealtimeOutput)
                .WithInputCodec(
                    playbackSettings.StreamSeek,
                    fillerKind == FillerKind.Fallback,
                    videoPath,
                    audioPath,
                    playbackSettings.VideoDecoder,
                    videoStream.Codec,
                    videoStream.PixelFormat)
                .WithWatermark(watermarkOptions, channel.FFmpegProfile.Resolution)
                .WithVideoTrackTimeScale(playbackSettings.VideoTrackTimeScale)
                .WithAlignedAudio(videoPath == audioPath ? playbackSettings.AudioDuration : Option<TimeSpan>.None)
                .WithNormalizeLoudness(playbackSettings.NormalizeLoudness);

            playbackSettings.ScaledSize.Match(
                scaledSize =>
                {
                    builder = builder.WithDeinterlace(playbackSettings.Deinterlace)
                        .WithScaling(scaledSize);

                    if (NeedToPad(channel.FFmpegProfile.Resolution, scaledSize))
                    {
                        builder = builder.WithBlackBars(channel.FFmpegProfile.Resolution);
                    }

                    builder = builder
                        .WithFilterComplex(
                            videoStream,
                            maybeAudioStream,
                            videoPath,
                            audioPath,
                            channel.FFmpegProfile.VideoCodec);
                },
                () =>
                {
                    if (playbackSettings.PadToDesiredResolution)
                    {
                        builder = builder
                            .WithDeinterlace(playbackSettings.Deinterlace)
                            .WithBlackBars(channel.FFmpegProfile.Resolution)
                            .WithFilterComplex(
                                videoStream,
                                maybeAudioStream,
                                videoPath,
                                audioPath,
                                channel.FFmpegProfile.VideoCodec);
                    }
                    else if (playbackSettings.Deinterlace)
                    {
                        builder = builder.WithDeinterlace(playbackSettings.Deinterlace)
                            .WithAlignedAudio(playbackSettings.AudioDuration)
                            .WithFilterComplex(
                                videoStream,
                                maybeAudioStream,
                                videoPath,
                                audioPath,
                                channel.FFmpegProfile.VideoCodec);
                    }
                    else
                    {
                        builder = builder
                            .WithFilterComplex(
                                videoStream,
                                maybeAudioStream,
                                videoPath,
                                audioPath,
                                channel.FFmpegProfile.VideoCodec);
                    }
                });

            builder = builder.WithPlaybackArgs(playbackSettings)
                .WithMetadata(channel, maybeAudioStream)
                .WithDuration(finish - now);

            switch (channel.StreamingMode)
            {
                // HLS needs to segment and generate playlist
                case StreamingMode.HttpLiveStreamingSegmenter:
                    return builder.WithHls(channel.Number, videoVersion)
                        .WithRealtimeOutput(hlsRealtime)
                        .Build();
                default:
                    return builder.WithFormat("mpegts")
                        .WithPipe()
                        .Build();
            }
        }

        public async Task<Process> ForError(
            string ffmpegPath,
            Channel channel,
            Option<TimeSpan> duration,
            string errorMessage,
            bool hlsRealtime)
        {
            FFmpegPlaybackSettings playbackSettings =
                _playbackSettingsCalculator.CalculateErrorSettings(channel.FFmpegProfile);

            IDisplaySize desiredResolution = channel.FFmpegProfile.Resolution;

            var fontSize = (int)Math.Round(channel.FFmpegProfile.Resolution.Height / 20.0);
            var margin = (int)Math.Round(channel.FFmpegProfile.Resolution.Height * 0.05);
            
            string subtitleFile = await new SubtitleBuilder(_tempFilePool)
                .WithResolution(desiredResolution)
                .WithFontName("Roboto")
                .WithFontSize(fontSize)
                .WithAlignment(2)
                .WithMarginV(margin)
                .WithPrimaryColor("&HFFFFFF")
                .WithFormattedContent(errorMessage.Replace(Environment.NewLine, "\\N"))
                .BuildFile();

            var videoStream = new MediaStream { Index = 0 };
            var audioStream = new MediaStream { Index = 0 };

            FFmpegProcessBuilder builder = new FFmpegProcessBuilder(ffmpegPath, false, _logger)
                .WithThreads(1)
                .WithQuiet()
                .WithFormatFlags(playbackSettings.FormatFlags)
                .WithRealtimeOutput(playbackSettings.RealtimeOutput)
                .WithLoopedImage(Path.Combine(FileSystemLayout.ResourcesCacheFolder, "background.png"))
                .WithLibavfilter()
                .WithInput("anullsrc")
                .WithSubtitleFile(subtitleFile)
                .WithFilterComplex(
                    videoStream,
                    audioStream,
                    Path.Combine(FileSystemLayout.ResourcesCacheFolder, "background.png"),
                    "fake-audio-path",
                    playbackSettings.VideoCodec)
                .WithPixfmt("yuv420p")
                .WithPlaybackArgs(playbackSettings)
                .WithMetadata(channel, None);

            await duration.IfSomeAsync(d => builder = builder.WithDuration(d));

            switch (channel.StreamingMode)
            {
                // HLS needs to segment and generate playlist
                case StreamingMode.HttpLiveStreamingSegmenter:
                    return builder.WithHls(channel.Number, None)
                        .WithRealtimeOutput(hlsRealtime)
                        .Build();
                default:
                    return builder.WithFormat("mpegts")
                        .WithPipe()
                        .Build();
            }
        }

        public Process ConcatChannel(string ffmpegPath, bool saveReports, Channel channel, string scheme, string host)
        {
            FFmpegPlaybackSettings playbackSettings = _playbackSettingsCalculator.ConcatSettings;

            return new FFmpegProcessBuilder(ffmpegPath, saveReports, _logger)
                .WithThreads(1)
                .WithQuiet()
                .WithFormatFlags(playbackSettings.FormatFlags)
                .WithRealtimeOutput(playbackSettings.RealtimeOutput)
                .WithInfiniteLoop()
                .WithConcat($"http://localhost:{Settings.ListenPort}/ffmpeg/concat/{channel.Number}")
                .WithMetadata(channel, None)
                .WithFormat("mpegts")
                .WithPipe()
                .Build();
        }

        public Process ConvertToPng(string ffmpegPath, string inputFile, string outputFile)
        {
            return new FFmpegProcessBuilder(ffmpegPath, false, _logger)
                .WithThreads(1)
                .WithQuiet()
                .WithInput(inputFile)
                .WithOutputFormat("apng", outputFile)
                .Build();
        }

        public Process ExtractAttachedPicAsPng(string ffmpegPath, string inputFile, int streamIndex, string outputFile)
        {
            return new FFmpegProcessBuilder(ffmpegPath, false, _logger)
                .WithThreads(1)
                .WithQuiet()
                .WithInput(inputFile)
                .WithMap($"0:{streamIndex}")
                .WithOutputFormat("apng", outputFile)
                .Build();
        }

        public async Task<Either<BaseError, string>> GenerateSongImage(
            string ffmpegPath,
            Option<string> subtitleFile,
            Channel channel,
            Option<ChannelWatermark> globalWatermark,
            MediaVersion videoVersion,
            string videoPath,
            bool boxBlur,
            Option<string> watermarkPath,
            ChannelWatermarkLocation watermarkLocation,
            int horizontalMarginPercent,
            int verticalMarginPercent,
            int watermarkWidthPercent)
        {
            try
            {
                string outputFile = _tempFilePool.GetNextTempFile(TempFileCategory.SongBackground);

                MediaStream videoStream = await _ffmpegStreamSelector.SelectVideoStream(channel, videoVersion);

                Option<ChannelWatermark> watermarkOverride =
                    videoVersion is FallbackMediaVersion or CoverArtMediaVersion
                        ? new ChannelWatermark
                        {
                            Mode = ChannelWatermarkMode.Permanent,
                            HorizontalMarginPercent = horizontalMarginPercent,
                            VerticalMarginPercent = verticalMarginPercent,
                            Location = watermarkLocation,
                            Size = ChannelWatermarkSize.Scaled,
                            WidthPercent = watermarkWidthPercent,
                            Opacity = 100
                        }
                        : None;

                Option<WatermarkOptions> watermarkOptions =
                    await GetWatermarkOptions(channel, globalWatermark, videoVersion, watermarkOverride, watermarkPath);

                FFmpegPlaybackSettings playbackSettings =
                    _playbackSettingsCalculator.CalculateErrorSettings(channel.FFmpegProfile);
                
                FFmpegPlaybackSettings scalePlaybackSettings = _playbackSettingsCalculator.CalculateSettings(
                    StreamingMode.TransportStream,
                    channel.FFmpegProfile,
                    videoVersion,
                    videoStream,
                    None,
                    DateTimeOffset.UnixEpoch,
                    DateTimeOffset.UnixEpoch,
                    TimeSpan.Zero,
                    TimeSpan.Zero);

                FFmpegProcessBuilder builder = new FFmpegProcessBuilder(ffmpegPath, false, _logger)
                    .WithThreads(1)
                    .WithQuiet()
                    .WithFormatFlags(playbackSettings.FormatFlags)
                    .WithSongInput(videoPath, videoStream.Codec, videoStream.PixelFormat, boxBlur)
                    .WithWatermark(watermarkOptions, channel.FFmpegProfile.Resolution)
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
                        playbackSettings.VideoCodec)
                    .WithOutputFormat("apng", outputFile)
                    .Build();

                _logger.LogInformation(
                    "ffmpeg song arguments {FFmpegArguments}",
                    string.Join(" ", process.StartInfo.ArgumentList));

                process.Start();
                await process.WaitForExitAsync();

                return outputFile;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating song image");
                return Left(BaseError.New(ex.Message));
            }
        }

        private bool NeedToPad(IDisplaySize target, IDisplaySize displaySize) =>
            displaySize.Width != target.Width || displaySize.Height != target.Height;

        private async Task<WatermarkOptions> GetWatermarkOptions(
            Channel channel,
            Option<ChannelWatermark> globalWatermark,
            MediaVersion videoVersion,
            Option<ChannelWatermark> watermarkOverride,
            Option<string> watermarkPath)
        {
            if (videoVersion is BackgroundImageMediaVersion)
            {
                return new WatermarkOptions(None, None, None, false);
            }

            if (channel.StreamingMode != StreamingMode.HttpLiveStreamingDirect && channel.FFmpegProfile.Transcode &&
                channel.FFmpegProfile.NormalizeVideo)
            {
                if (videoVersion is CoverArtMediaVersion)
                {
                    return new WatermarkOptions(
                        watermarkOverride,
                        await watermarkPath.IfNoneAsync(videoVersion.MediaFiles.Head().Path),
                        0,
                        false);
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
                                await _imageCache.IsAnimated(customPath));
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
                                    p => _imageCache.IsAnimated(p),
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
                                await _imageCache.IsAnimated(customPath));
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
                                    p => _imageCache.IsAnimated(p),
                                    () => Task.FromResult(false)));
                        default:
                            throw new NotSupportedException("Unsupported watermark image source");
                    }
                }
            }

            return new WatermarkOptions(None, None, None, false);
        }
    }
}
