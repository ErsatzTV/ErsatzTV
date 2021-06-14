using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Images;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.FFmpeg
{
    public class FFmpegProcessService
    {
        private readonly IFFmpegStreamSelector _ffmpegStreamSelector;
        private readonly IImageCache _imageCache;
        private readonly IConfigElementRepository _configElementRepository;
        private readonly FFmpegPlaybackSettingsCalculator _playbackSettingsCalculator;

        public FFmpegProcessService(
            IConfigElementRepository configElementRepository,
            FFmpegPlaybackSettingsCalculator ffmpegPlaybackSettingsService,
            IFFmpegStreamSelector ffmpegStreamSelector,
            IImageCache imageCache)
        {
            _configElementRepository = configElementRepository;
            _playbackSettingsCalculator = ffmpegPlaybackSettingsService;
            _ffmpegStreamSelector = ffmpegStreamSelector;
            _imageCache = imageCache;
        }

        public async Task<Process> ForPlayoutItem(
            string ffmpegPath,
            bool saveReports,
            Channel channel,
            MediaVersion version,
            string path,
            DateTimeOffset start,
            DateTimeOffset now)
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

            Option<string> maybeWatermarkPath = None;

            if (channel.StreamingMode != StreamingMode.HttpLiveStreamingDirect)
            {
                // check for channel watermark
                maybeWatermarkPath = channel.Artwork
                    .Filter(a => a.ArtworkKind == ArtworkKind.Watermark)
                    .HeadOrNone()
                    .Map(a => _imageCache.GetPathForImage(a.Path, ArtworkKind.Watermark, Option<int>.None));

                // check for global watermark
                if (maybeWatermarkPath.IsNone)
                {
                    maybeWatermarkPath = await _configElementRepository
                        .GetValue<string>(ConfigElementKey.FFmpegWatermark)
                        .MapT(a => _imageCache.GetPathForImage(a, ArtworkKind.Watermark, Option<int>.None));
                }

                // finally, check for channel logo
                if (maybeWatermarkPath.IsNone)
                {
                    maybeWatermarkPath = channel.Artwork
                        .Filter(a => a.ArtworkKind == ArtworkKind.Logo)
                        .HeadOrNone()
                        .Map(a => _imageCache.GetPathForImage(a.Path, ArtworkKind.Logo, Option<int>.None));
                }
            }

            bool isAnimated = await maybeWatermarkPath.Match(
                p => _imageCache.IsAnimated(p),
                () => Task.FromResult(false));

            Option<ChannelWatermark> maybeWatermark = channel.Watermark;

            FFmpegProcessBuilder builder = new FFmpegProcessBuilder(ffmpegPath, saveReports)
                .WithThreads(playbackSettings.ThreadCount)
                .WithHardwareAcceleration(playbackSettings.HardwareAcceleration)
                .WithQuiet()
                .WithFormatFlags(playbackSettings.FormatFlags)
                .WithRealtimeOutput(playbackSettings.RealtimeOutput)
                .WithSeek(playbackSettings.StreamSeek)
                .WithInputCodec(path, playbackSettings.HardwareAcceleration, videoStream.Codec)
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
                        .WithFilterComplex(videoStream, maybeAudioStream);
                },
                () =>
                {
                    if (playbackSettings.PadToDesiredResolution)
                    {
                        builder = builder
                            .WithDeinterlace(playbackSettings.Deinterlace)
                            .WithBlackBars(channel.FFmpegProfile.Resolution)
                            .WithFilterComplex(videoStream, maybeAudioStream);
                    }
                    else if (playbackSettings.Deinterlace)
                    {
                        builder = builder.WithDeinterlace(playbackSettings.Deinterlace)
                            .WithAlignedAudio(playbackSettings.AudioDuration)
                            .WithFilterComplex(videoStream, maybeAudioStream);
                    }
                    else
                    {
                        builder = builder
                            .WithFilterComplex(videoStream, maybeAudioStream);
                    }
                });

            return builder.WithPlaybackArgs(playbackSettings)
                .WithMetadata(channel)
                .WithFormat("mpegts")
                .WithDuration(start + version.Duration - now)
                .WithPipe()
                .Build();
        }

        public Process ForError(string ffmpegPath, Channel channel, Option<TimeSpan> duration, string errorMessage)
        {
            FFmpegPlaybackSettings playbackSettings =
                _playbackSettingsCalculator.CalculateErrorSettings(channel.FFmpegProfile);

            IDisplaySize desiredResolution = channel.FFmpegProfile.Resolution;

            FFmpegProcessBuilder builder = new FFmpegProcessBuilder(ffmpegPath, false)
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
                .WithMetadata(channel)
                .WithFormat("mpegts");

            duration.IfSome(d => builder = builder.WithDuration(d));

            return builder.WithPipe().Build();
        }

        public Process ConcatChannel(string ffmpegPath, bool saveReports, Channel channel, string scheme, string host)
        {
            FFmpegPlaybackSettings playbackSettings = _playbackSettingsCalculator.ConcatSettings;

            return new FFmpegProcessBuilder(ffmpegPath, saveReports)
                .WithThreads(1)
                .WithQuiet()
                .WithFormatFlags(playbackSettings.FormatFlags)
                .WithRealtimeOutput(playbackSettings.RealtimeOutput)
                .WithInfiniteLoop()
                .WithConcat($"http://localhost:{Settings.ListenPort}/ffmpeg/concat/{channel.Number}")
                .WithMetadata(channel)
                .WithFormat("mpegts")
                .WithPipe()
                .Build();
        }

        private bool NeedToPad(IDisplaySize target, IDisplaySize displaySize) =>
            displaySize.Width != target.Width || displaySize.Height != target.Height;
    }
}
