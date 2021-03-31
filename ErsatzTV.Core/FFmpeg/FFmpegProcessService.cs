﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using LanguageExt;

namespace ErsatzTV.Core.FFmpeg
{
    public class FFmpegProcessService
    {
        private readonly IFFmpegStreamSelector _ffmpegStreamSelector;
        private readonly FFmpegPlaybackSettingsCalculator _playbackSettingsCalculator;

        public FFmpegProcessService(
            FFmpegPlaybackSettingsCalculator ffmpegPlaybackSettingsService,
            IFFmpegStreamSelector ffmpegStreamSelector)
        {
            _playbackSettingsCalculator = ffmpegPlaybackSettingsService;
            _ffmpegStreamSelector = ffmpegStreamSelector;
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
            MediaStream audioStream = await _ffmpegStreamSelector.SelectAudioStream(channel, version);

            FFmpegPlaybackSettings playbackSettings = _playbackSettingsCalculator.CalculateSettings(
                channel.StreamingMode,
                channel.FFmpegProfile,
                version,
                videoStream,
                audioStream,
                start,
                now);

            FFmpegProcessBuilder builder = new FFmpegProcessBuilder(ffmpegPath, saveReports)
                .WithThreads(playbackSettings.ThreadCount)
                .WithHardwareAcceleration(playbackSettings.HardwareAcceleration)
                .WithQuiet()
                .WithFormatFlags(playbackSettings.FormatFlags)
                .WithRealtimeOutput(playbackSettings.RealtimeOutput)
                .WithSeek(playbackSettings.StreamSeek)
                .WithInputCodec(path, playbackSettings.HardwareAcceleration, videoStream.Codec);

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
                        .WithAlignedAudio(playbackSettings.AudioDuration)
                        .WithFilterComplex(videoStream.Index, audioStream.Index);
                },
                () =>
                {
                    if (playbackSettings.PadToDesiredResolution)
                    {
                        builder = builder
                            .WithDeinterlace(playbackSettings.Deinterlace)
                            .WithBlackBars(channel.FFmpegProfile.Resolution)
                            .WithAlignedAudio(playbackSettings.AudioDuration)
                            .WithFilterComplex(videoStream.Index, audioStream.Index);
                    }
                    else if (playbackSettings.Deinterlace)
                    {
                        builder = builder.WithDeinterlace(playbackSettings.Deinterlace)
                            .WithAlignedAudio(playbackSettings.AudioDuration)
                            .WithFilterComplex(videoStream.Index, audioStream.Index);
                    }
                    else
                    {
                        builder = builder
                            .WithAlignedAudio(playbackSettings.AudioDuration)
                            .WithFilterComplex(videoStream.Index, audioStream.Index);
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

        public Process ConcatChannel(string ffmpegPath, bool saveReports, Channel channel, string scheme, string host)
        {
            FFmpegPlaybackSettings playbackSettings = _playbackSettingsCalculator.ConcatSettings;

            return new FFmpegProcessBuilder(ffmpegPath, saveReports)
                .WithThreads(1)
                .WithQuiet()
                .WithFormatFlags(playbackSettings.FormatFlags)
                .WithRealtimeOutput(playbackSettings.RealtimeOutput)
                .WithInfiniteLoop()
                .WithConcat($"http://localhost:8409/ffmpeg/concat/{channel.Number}")
                .WithMetadata(channel)
                .WithFormat("mpegts")
                .WithPipe()
                .Build();
        }

        private bool NeedToPad(IDisplaySize target, IDisplaySize displaySize) =>
            displaySize.Width != target.Width || displaySize.Height != target.Height;
    }
}
