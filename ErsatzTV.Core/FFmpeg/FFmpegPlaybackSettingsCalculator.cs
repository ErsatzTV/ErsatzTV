// zlib License
//
// Copyright (c) 2021 Dan Ferguson, Victor Hugo Soliz Kuncar, Jason Dove
//
// This software is provided 'as-is', without any express or implied
// warranty. In no event will the authors be held liable for any damages
// arising from the use of this software.
//
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
//
// 1. The origin of this software must not be misrepresented; you must not
//    claim that you wrote the original software. If you use this software
//    in a product, an acknowledgment in the product documentation would be
//    appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
//    misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.

using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.FFmpeg
{
    public class FFmpegPlaybackSettingsCalculator
    {
        private static readonly List<string> CommonFormatFlags = new()
        {
            "+genpts",
            "+discardcorrupt",
            "+igndts"
        };

        public FFmpegPlaybackSettings ConcatSettings => new()
        {
            ThreadCount = 1,
            FormatFlags = CommonFormatFlags
        };

        public FFmpegPlaybackSettings CalculateSettings(
            StreamingMode streamingMode,
            FFmpegProfile ffmpegProfile,
            MediaVersion version,
            MediaStream videoStream,
            Option<MediaStream> audioStream,
            DateTimeOffset start,
            DateTimeOffset now)
        {
            var result = new FFmpegPlaybackSettings
            {
                ThreadCount = ffmpegProfile.ThreadCount,
                FormatFlags = CommonFormatFlags
            };

            if (now != start)
            {
                result.StreamSeek = now - start;
            }

            switch (streamingMode)
            {
                case StreamingMode.HttpLiveStreamingDirect:
                    result.AudioCodec = "copy";
                    result.VideoCodec = "copy";
                    result.Deinterlace = false;
                    break;
                case StreamingMode.HttpLiveStreamingHybrid:
                case StreamingMode.TransportStream:
                    result.HardwareAcceleration = ffmpegProfile.HardwareAcceleration;

                    if (NeedToScale(ffmpegProfile, version))
                    {
                        IDisplaySize scaledSize = CalculateScaledSize(ffmpegProfile, version);
                        if (!scaledSize.IsSameSizeAs(version))
                        {
                            int fixedHeight = scaledSize.Height + scaledSize.Height % 2;
                            int fixedWidth = scaledSize.Width + scaledSize.Width % 2;
                            result.ScaledSize = Some((IDisplaySize) new DisplaySize(fixedWidth, fixedHeight));
                        }
                    }

                    IDisplaySize sizeAfterScaling = result.ScaledSize.IfNone(version);
                    if (ffmpegProfile.Transcode && ffmpegProfile.NormalizeVideo && !sizeAfterScaling.IsSameSizeAs(ffmpegProfile.Resolution))
                    {
                        result.PadToDesiredResolution = true;
                    }

                    if (ffmpegProfile.Transcode && ffmpegProfile.NormalizeVideo)
                    {
                        result.VideoTrackTimeScale = 90000;
                    }

                    if (result.ScaledSize.IsSome || result.PadToDesiredResolution ||
                        NeedToNormalizeVideoCodec(ffmpegProfile, videoStream))
                    {
                        result.VideoCodec = ffmpegProfile.VideoCodec;
                        result.VideoBitrate = ffmpegProfile.VideoBitrate;
                        result.VideoBufferSize = ffmpegProfile.VideoBufferSize;
                    }
                    else
                    {
                        result.VideoCodec = "copy";
                    }

                    if (ffmpegProfile.Transcode && ffmpegProfile.NormalizeAudio)
                    {
                        result.AudioCodec = ffmpegProfile.AudioCodec;
                        result.AudioBitrate = ffmpegProfile.AudioBitrate;
                        result.AudioBufferSize = ffmpegProfile.AudioBufferSize;

                        audioStream.IfSome(
                            stream =>
                            {
                                if (stream.Channels != ffmpegProfile.AudioChannels)
                                {
                                    result.AudioChannels = ffmpegProfile.AudioChannels;
                                }
                            });

                        result.AudioSampleRate = ffmpegProfile.AudioSampleRate;
                        result.AudioDuration = version.Duration;
                        result.NormalizeLoudness = ffmpegProfile.NormalizeLoudness;
                    }
                    else
                    {
                        result.AudioCodec = "copy";
                    }

                    if (version.VideoScanKind == VideoScanKind.Interlaced)
                    {
                        result.Deinterlace = true;
                    }

                    break;
            }

            return result;
        }

        public FFmpegPlaybackSettings CalculateErrorSettings(FFmpegProfile ffmpegProfile) =>
            new()
            {
                ThreadCount = ffmpegProfile.ThreadCount,
                FormatFlags = CommonFormatFlags,
                VideoCodec = ffmpegProfile.VideoCodec,
                AudioCodec = ffmpegProfile.AudioCodec
            };

        private static bool NeedToScale(FFmpegProfile ffmpegProfile, MediaVersion version) =>
            ffmpegProfile.Transcode && ffmpegProfile.NormalizeVideo &&
            IsIncorrectSize(ffmpegProfile.Resolution, version) ||
            IsTooLarge(ffmpegProfile.Resolution, version) ||
            IsOddSize(version);

        private static bool IsIncorrectSize(IDisplaySize desiredResolution, MediaVersion version) =>
            IsAnamorphic(version) ||
            version.Width != desiredResolution.Width ||
            version.Height != desiredResolution.Height;

        private static bool IsTooLarge(IDisplaySize desiredResolution, IDisplaySize displaySize) =>
            displaySize.Height > desiredResolution.Height ||
            displaySize.Width > desiredResolution.Width;

        private static bool IsOddSize(MediaVersion version) =>
            version.Height % 2 == 1 || version.Width % 2 == 1;

        private static bool NeedToNormalizeVideoCodec(FFmpegProfile ffmpegProfile, MediaStream videoStream) =>
            ffmpegProfile.Transcode && ffmpegProfile.NormalizeVideo && ffmpegProfile.VideoCodec != videoStream.Codec;

        private static IDisplaySize CalculateScaledSize(FFmpegProfile ffmpegProfile, MediaVersion version)
        {
            IDisplaySize sarSize = SARSize(version);
            int p = version.Width * sarSize.Width;
            int q = version.Height * sarSize.Height;
            int g = Gcd(q, p);
            p = p / g;
            q = q / g;
            IDisplaySize targetSize = ffmpegProfile.Resolution;
            int hw1 = targetSize.Width;
            int hh1 = hw1 * q / p;
            int hh2 = targetSize.Height;
            int hw2 = targetSize.Height * p / q;
            if (hh1 <= targetSize.Height)
            {
                return new DisplaySize(hw1, hh1);
            }

            return new DisplaySize(hw2, hh2);
        }

        private static int Gcd(int a, int b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                {
                    a %= b;
                }
                else
                {
                    b %= a;
                }
            }

            return a | b;
        }

        private static bool IsAnamorphic(MediaVersion version)
        {
            if (version.SampleAspectRatio == "1:1")
            {
                return false;
            }

            if (version.SampleAspectRatio != "0:1")
            {
                return true;
            }

            if (version.DisplayAspectRatio == "0:1")
            {
                return false;
            }

            return version.DisplayAspectRatio != $"{version.Width}:{version.Height}";
        }

        private static IDisplaySize SARSize(MediaVersion version)
        {
            string[] split = version.SampleAspectRatio.Split(":");
            return new DisplaySize(int.Parse(split[0]), int.Parse(split[1]));
        }
    }
}
