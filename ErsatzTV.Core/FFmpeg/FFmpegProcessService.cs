using System;
using System.Diagnostics;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.FFmpeg
{
    public class FFmpegProcessService
    {
        private readonly FFmpegPlaybackSettingsCalculator _playbackSettingsCalculator;

        public FFmpegProcessService(FFmpegPlaybackSettingsCalculator ffmpegPlaybackSettingsService) =>
            _playbackSettingsCalculator = ffmpegPlaybackSettingsService;

        public Process ForPlayoutItem(
            string ffmpegPath,
            Channel channel,
            PlayoutItem item,
            DateTimeOffset now)
        {
            FFmpegPlaybackSettings playbackSettings = _playbackSettingsCalculator.CalculateSettings(
                channel.StreamingMode,
                channel.FFmpegProfile,
                item,
                now);

            FFmpegProcessBuilder builder = new FFmpegProcessBuilder(ffmpegPath)
                .WithThreads(playbackSettings.ThreadCount)
                .WithQuiet()
                .WithFormatFlags(playbackSettings.FormatFlags)
                .WithRealtimeOutput(playbackSettings.RealtimeOutput)
                .WithSeek(playbackSettings.StreamSeek)
                .WithInput(item.MediaItem.Path);

            playbackSettings.ScaledSize.Match(
                scaledSize =>
                {
                    builder = builder.WithDeinterlace(playbackSettings.Deinterlace)
                        .WithScaling(scaledSize, playbackSettings.ScalingAlgorithm)
                        .WithSAR();

                    scaledSize = scaledSize.PadToEven();
                    if (NeedToPad(channel.FFmpegProfile.Resolution, scaledSize))
                    {
                        builder = builder.WithBlackBars(channel.FFmpegProfile.Resolution);
                    }

                    builder = builder
                        .WithAlignedAudio(playbackSettings.AudioDuration).WithFilterComplex();
                },
                () =>
                {
                    if (playbackSettings.PadToDesiredResolution)
                    {
                        builder = builder
                            .WithDeinterlace(playbackSettings.Deinterlace)
                            .WithSAR()
                            .WithBlackBars(channel.FFmpegProfile.Resolution)
                            .WithAlignedAudio(playbackSettings.AudioDuration)
                            .WithFilterComplex();
                    }
                    else if (playbackSettings.Deinterlace)
                    {
                        builder = builder.WithDeinterlace(playbackSettings.Deinterlace)
                            .WithAlignedAudio(playbackSettings.AudioDuration)
                            .WithFilterComplex();
                    }
                    else
                    {
                        builder = builder
                            .WithAlignedAudio(playbackSettings.AudioDuration)
                            .WithFilterComplex();
                    }
                });

            return builder.WithPlaybackArgs(playbackSettings)
                .WithMetadata(channel)
                .WithFormat("mpegts")
                .WithDuration(item.Start + item.MediaItem.Metadata.Duration - now)
                .WithPipe()
                .Build();
        }

        public Process ForOfflineImage(string ffmpegPath, Channel channel)
        {
            FFmpegPlaybackSettings playbackSettings =
                _playbackSettingsCalculator.CalculateErrorSettings(channel.FFmpegProfile);

            IDisplaySize desiredResolution = channel.FFmpegProfile.Resolution;

            return new FFmpegProcessBuilder(ffmpegPath)
                .WithThreads(1)
                .WithQuiet()
                .WithFormatFlags(playbackSettings.FormatFlags)
                .WithRealtimeOutput(playbackSettings.RealtimeOutput)
                .WithLoopedImage("Resources/background.png")
                .WithLibavfilter()
                .WithInput("anullsrc")
                .WithFilterComplex(
                    $"[0:0]scale={desiredResolution.Width}:{desiredResolution.Height}[video]",
                    "[video]",
                    "1:a")
                .WithPixfmt("yuv420p")
                .WithPlaybackArgs(playbackSettings)
                .WithMetadata(channel)
                .WithFormat("mpegts")
                .WithDuration(TimeSpan.FromSeconds(10)) // TODO: figure out when we're back online
                .WithPipe()
                .Build();
        }

        public Process ConcatChannel(string ffmpegPath, Channel channel, string scheme, string host)
        {
            FFmpegPlaybackSettings playbackSettings = _playbackSettingsCalculator.ConcatSettings;

            return new FFmpegProcessBuilder(ffmpegPath)
                .WithThreads(1)
                .WithQuiet()
                .WithFormatFlags(playbackSettings.FormatFlags)
                .WithRealtimeOutput(playbackSettings.RealtimeOutput)
                .WithInfiniteLoop()
                .WithConcat($"{scheme}://{host}/ffmpeg/concat/{channel.Number}")
                .WithMetadata(channel)
                .WithFormat("mpegts")
                .WithPipe()
                .Build();
        }

        private bool NeedToPad(IDisplaySize target, IDisplaySize displaySize) =>
            displaySize.Width != target.Width || displaySize.Height != target.Height;
    }
}
