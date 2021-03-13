using System;
using System.Diagnostics;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using LanguageExt;

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
            MediaVersion version,
            string path,
            DateTimeOffset start,
            DateTimeOffset now)
        {
            FFmpegPlaybackSettings playbackSettings = _playbackSettingsCalculator.CalculateSettings(
                channel.StreamingMode,
                channel.FFmpegProfile,
                version,
                start,
                now);

            FFmpegProcessBuilder builder = new FFmpegProcessBuilder(ffmpegPath)
                .WithThreads(playbackSettings.ThreadCount)
                .WithHardwareAcceleration(playbackSettings.HardwareAcceleration)
                .WithQuiet()
                .WithFormatFlags(playbackSettings.FormatFlags)
                .WithRealtimeOutput(playbackSettings.RealtimeOutput)
                .WithSeek(playbackSettings.StreamSeek)
                .WithInputCodec(path, playbackSettings.HardwareAcceleration, version.VideoCodec);

            playbackSettings.ScaledSize.Match(
                scaledSize =>
                {
                    builder = builder.WithDeinterlace(playbackSettings.Deinterlace)
                        .WithScaling(scaledSize);

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
                .WithDuration(start + version.Duration - now)
                .WithPipe()
                .Build();
        }

        public Process ForError(string ffmpegPath, Channel channel, Option<TimeSpan> duration, string errorMessage)
        {
            FFmpegPlaybackSettings playbackSettings =
                _playbackSettingsCalculator.CalculateErrorSettings(channel.FFmpegProfile);

            IDisplaySize desiredResolution = channel.FFmpegProfile.Resolution;

            FFmpegProcessBuilder builder = new FFmpegProcessBuilder(ffmpegPath)
                .WithThreads(1)
                .WithQuiet()
                .WithFormatFlags(playbackSettings.FormatFlags)
                .WithRealtimeOutput(playbackSettings.RealtimeOutput)
                .WithLoopedImage("Resources/background.png")
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
