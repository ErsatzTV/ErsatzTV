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
            PlayoutItem playoutItem,
            DateTimeOffset now)
        {
            var result = new FFmpegPlaybackSettings
            {
                ThreadCount = ffmpegProfile.ThreadCount,
                FormatFlags = CommonFormatFlags
            };

            if (now != playoutItem.Start)
            {
                result.StreamSeek = now - playoutItem.Start;
            }

            switch (streamingMode)
            {
                case StreamingMode.HttpLiveStreaming:
                    result.AudioCodec = "copy";
                    result.VideoCodec = "copy";
                    result.Deinterlace = false;
                    break;
                case StreamingMode.TransportStream:
                    if (NeedToScale(ffmpegProfile, playoutItem.MediaItem.Metadata))
                    {
                        IDisplaySize scaledSize = CalculateScaledSize(ffmpegProfile, playoutItem.MediaItem.Metadata);
                        if (!scaledSize.IsSameSizeAs(playoutItem.MediaItem.Metadata))
                        {
                            result.ScaledSize = Some(
                                CalculateScaledSize(ffmpegProfile, playoutItem.MediaItem.Metadata));
                        }
                    }

                    IDisplaySize sizeAfterScaling = result.ScaledSize.IfNone(playoutItem.MediaItem.Metadata);
                    if (!sizeAfterScaling.IsSameSizeAs(ffmpegProfile.Resolution))
                    {
                        result.PadToDesiredResolution = true;
                    }

                    if (result.ScaledSize.IsSome || result.PadToDesiredResolution ||
                        NeedToNormalizeVideoCodec(ffmpegProfile, playoutItem.MediaItem.Metadata))
                    {
                        result.VideoCodec = ffmpegProfile.VideoCodec;
                        result.VideoBitrate = ffmpegProfile.VideoBitrate;
                        result.VideoBufferSize = ffmpegProfile.VideoBufferSize;
                    }
                    else
                    {
                        result.VideoCodec = "copy";
                    }

                    if (NeedToNormalizeAudioCodec(ffmpegProfile, playoutItem.MediaItem.Metadata))
                    {
                        result.AudioCodec = ffmpegProfile.AudioCodec;
                        result.AudioBitrate = ffmpegProfile.AudioBitrate;
                        result.AudioBufferSize = ffmpegProfile.AudioBufferSize;

                        if (ffmpegProfile.NormalizeAudio)
                        {
                            result.AudioChannels = ffmpegProfile.AudioChannels;
                            result.AudioSampleRate = ffmpegProfile.AudioSampleRate;
                            result.AudioDuration = playoutItem.MediaItem.Metadata.Duration;
                        }
                    }
                    else
                    {
                        result.AudioCodec = "copy";
                    }

                    if (playoutItem.MediaItem.Metadata.VideoScanType == VideoScanType.Interlaced)
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

        private static bool NeedToScale(FFmpegProfile ffmpegProfile, MediaMetadata mediaMetadata) =>
            ffmpegProfile.NormalizeResolution &&
            IsIncorrectSize(ffmpegProfile.Resolution, mediaMetadata) ||
            IsTooLarge(ffmpegProfile.Resolution, mediaMetadata) ||
            IsOddSize(mediaMetadata);

        private static bool IsIncorrectSize(IDisplaySize desiredResolution, MediaMetadata mediaMetadata) =>
            IsAnamorphic(mediaMetadata) ||
            mediaMetadata.Width != desiredResolution.Width ||
            mediaMetadata.Height != desiredResolution.Height;

        private static bool IsTooLarge(IDisplaySize desiredResolution, IDisplaySize mediaSize) =>
            mediaSize.Height > desiredResolution.Height ||
            mediaSize.Width > desiredResolution.Width;

        private static bool IsOddSize(IDisplaySize displaySize) =>
            displaySize.Height % 2 == 1 || displaySize.Width % 2 == 1;

        private static bool NeedToNormalizeVideoCodec(FFmpegProfile ffmpegProfile, MediaMetadata mediaMetadata) =>
            ffmpegProfile.NormalizeVideoCodec && ffmpegProfile.VideoCodec != mediaMetadata.VideoCodec;

        private static bool NeedToNormalizeAudioCodec(FFmpegProfile ffmpegProfile, MediaMetadata mediaMetadata) =>
            ffmpegProfile.NormalizeAudioCodec && ffmpegProfile.AudioCodec != mediaMetadata.AudioCodec;

        private static IDisplaySize CalculateScaledSize(FFmpegProfile ffmpegProfile, MediaMetadata mediaMetadata)
        {
            IDisplaySize sarSize = SARSize(mediaMetadata);
            int p = mediaMetadata.Width * sarSize.Width;
            int q = mediaMetadata.Height * sarSize.Height;
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

        private static bool IsAnamorphic(MediaMetadata mediaMetadata)
        {
            if (mediaMetadata.SampleAspectRatio == "1:1")
            {
                return false;
            }

            if (mediaMetadata.SampleAspectRatio != "0:1")
            {
                return true;
            }

            if (mediaMetadata.DisplayAspectRatio == "0:1")
            {
                return false;
            }

            return mediaMetadata.DisplayAspectRatio != $"{mediaMetadata.Width}:{mediaMetadata.Height}";
        }

        private static IDisplaySize SARSize(MediaMetadata mediaMetadata)
        {
            string[] split = mediaMetadata.SampleAspectRatio.Split(":");
            return new DisplaySize(int.Parse(split[0]), int.Parse(split[1]));
        }
    }
}
