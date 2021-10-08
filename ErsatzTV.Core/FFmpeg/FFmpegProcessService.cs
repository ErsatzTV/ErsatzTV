using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.FFmpeg
{
    public class FFmpegProcessService
    {
        private readonly IFFmpegStreamSelector _ffmpegStreamSelector;
        private readonly IImageCache _imageCache;
        private readonly ILogger<FFmpegProcessService> _logger;
        private readonly FFmpegPlaybackSettingsCalculator _playbackSettingsCalculator;

        public FFmpegProcessService(
            FFmpegPlaybackSettingsCalculator ffmpegPlaybackSettingsService,
            IFFmpegStreamSelector ffmpegStreamSelector,
            IImageCache imageCache,
            ILogger<FFmpegProcessService> logger)
        {
            _playbackSettingsCalculator = ffmpegPlaybackSettingsService;
            _ffmpegStreamSelector = ffmpegStreamSelector;
            _imageCache = imageCache;
            _logger = logger;
        }

        public async Task<Process> ForPlayoutItem(
            string ffmpegPath,
            bool saveReports,
            Channel channel,
            MediaVersion version,
            string path,
            DateTimeOffset start,
            DateTimeOffset now,
            Option<ChannelWatermark> globalWatermark,
            Option<VaapiDriver> maybeVaapiDriver)
        {
            MediaStream videoStream = await _ffmpegStreamSelector.SelectVideoStream(channel, version);
            Option<MediaStream> maybeAudioStream = await _ffmpegStreamSelector.SelectAudioStream(channel, version);

            FFmpegPlaybackSettings playbackSettings = _playbackSettingsCalculator.CalculateSettings(
                channel.StreamingMode,
                channel.FFmpegProfile,
                version,
                videoStream,
                maybeAudioStream,
                start,
                now);

            (Option<ChannelWatermark> maybeWatermark, Option<string> maybeWatermarkPath) =
                GetWatermarkOptions(channel, globalWatermark);

            bool isAnimated = await maybeWatermarkPath.Match(
                p => _imageCache.IsAnimated(p),
                () => Task.FromResult(false));

            FFmpegProcessBuilder builder = new FFmpegProcessBuilder(ffmpegPath, saveReports, _logger)
                .WithThreads(playbackSettings.ThreadCount)
                .WithHardwareAcceleration(playbackSettings.HardwareAcceleration)
                .WithVaapiDriver(maybeVaapiDriver)
                .WithQuiet()
                .WithFormatFlags(playbackSettings.FormatFlags)
                .WithRealtimeOutput(playbackSettings.RealtimeOutput)
                .WithSeek(playbackSettings.StreamSeek)
                .WithInputCodec(path, playbackSettings.HardwareAcceleration, videoStream.Codec, videoStream.PixelFormat)
                .WithWatermark(maybeWatermark, maybeWatermarkPath, channel.FFmpegProfile.Resolution, isAnimated)
                .WithVideoTrackTimeScale(playbackSettings.VideoTrackTimeScale)
                .WithAlignedAudio(playbackSettings.AudioDuration)
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
                        .WithFilterComplex(videoStream, maybeAudioStream, channel.FFmpegProfile.VideoCodec);
                },
                () =>
                {
                    if (playbackSettings.PadToDesiredResolution)
                    {
                        builder = builder
                            .WithDeinterlace(playbackSettings.Deinterlace)
                            .WithBlackBars(channel.FFmpegProfile.Resolution)
                            .WithFilterComplex(videoStream, maybeAudioStream, channel.FFmpegProfile.VideoCodec);
                    }
                    else if (playbackSettings.Deinterlace)
                    {
                        builder = builder.WithDeinterlace(playbackSettings.Deinterlace)
                            .WithAlignedAudio(playbackSettings.AudioDuration)
                            .WithFilterComplex(videoStream, maybeAudioStream, channel.FFmpegProfile.VideoCodec);
                    }
                    else
                    {
                        builder = builder
                            .WithFilterComplex(videoStream, maybeAudioStream, channel.FFmpegProfile.VideoCodec);
                    }
                });

            builder = builder.WithPlaybackArgs(playbackSettings)
                .WithMetadata(channel, maybeAudioStream)
                .WithDuration(start + version.Duration - now);

            switch (channel.StreamingMode)
            {
                // HLS needs to segment and generate playlist
                case StreamingMode.HttpLiveStreamingSegmenter:
                    return builder.WithHls(channel.Number, version)
                        .Build();
                default:
                    return builder.WithFormat("mpegts")
                        .WithPipe()
                        .Build();
            }
        }

        public Process ForError(string ffmpegPath, Channel channel, Option<TimeSpan> duration, string errorMessage)
        {
            FFmpegPlaybackSettings playbackSettings =
                _playbackSettingsCalculator.CalculateErrorSettings(channel.FFmpegProfile);

            IDisplaySize desiredResolution = channel.FFmpegProfile.Resolution;

            FFmpegProcessBuilder builder = new FFmpegProcessBuilder(ffmpegPath, false, _logger)
                .WithThreads(1)
                .WithQuiet()
                .WithFormatFlags(playbackSettings.FormatFlags)
                .WithRealtimeOutput(playbackSettings.RealtimeOutput)
                .WithLoopedImage(Path.Combine(FileSystemLayout.ResourcesCacheFolder, "background.png"))
                .WithLibavfilter()
                .WithInput("anullsrc")
                .WithErrorText(desiredResolution, errorMessage)
                .WithPixfmt("yuv420p")
                .WithPlaybackArgs(playbackSettings)
                .WithMetadata(channel, None)
                .WithFormat("mpegts");

            duration.IfSome(d => builder = builder.WithDuration(d));

            return builder.WithPipe().Build();
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

        private bool NeedToPad(IDisplaySize target, IDisplaySize displaySize) =>
            displaySize.Width != target.Width || displaySize.Height != target.Height;

        private WatermarkOptions GetWatermarkOptions(Channel channel, Option<ChannelWatermark> globalWatermark)
        {
            if (channel.StreamingMode != StreamingMode.HttpLiveStreamingDirect && channel.FFmpegProfile.Transcode &&
                channel.FFmpegProfile.NormalizeVideo)
            {
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
                            return new WatermarkOptions(channel.Watermark, customPath);
                        case ChannelWatermarkImageSource.ChannelLogo:
                            Option<string> maybeChannelPath = channel.Artwork
                                .Filter(a => a.ArtworkKind == ArtworkKind.Logo)
                                .HeadOrNone()
                                .Map(a => _imageCache.GetPathForImage(a.Path, ArtworkKind.Logo, Option<int>.None));
                            return new WatermarkOptions(channel.Watermark, maybeChannelPath);
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
                            return new WatermarkOptions(watermark, customPath);
                        case ChannelWatermarkImageSource.ChannelLogo:
                            Option<string> maybeChannelPath = channel.Artwork
                                .Filter(a => a.ArtworkKind == ArtworkKind.Logo)
                                .HeadOrNone()
                                .Map(a => _imageCache.GetPathForImage(a.Path, ArtworkKind.Logo, Option<int>.None));
                            return new WatermarkOptions(watermark, maybeChannelPath);
                        default:
                            throw new NotSupportedException("Unsupported watermark image source");
                    }
                }
            }

            return new WatermarkOptions(None, None);
        }

        private record WatermarkOptions(Option<ChannelWatermark> Watermark, Option<string> ImagePath);
    }
}
