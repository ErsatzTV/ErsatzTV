// zlib License
//
// Copyright (c) 2022 Dan Ferguson, Victor Hugo Soliz Kuncar, Jason Dove
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

using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.Core.FFmpeg;

public class FFmpegPlaybackSettingsCalculator
{
    private static readonly List<string> CommonFormatFlags = new()
    {
        "+genpts",
        "+discardcorrupt",
        "+igndts"
    };

    private static readonly List<string> SegmenterFormatFlags = new()
    {
        "+discardcorrupt",
        "+igndts"
    };

    public FFmpegPlaybackSettings CalculateSettings(
        StreamingMode streamingMode,
        FFmpegProfile ffmpegProfile,
        MediaVersion videoVersion,
        Option<MediaStream> videoStream,
        Option<MediaStream> audioStream,
        DateTimeOffset start,
        DateTimeOffset now,
        TimeSpan inPoint,
        TimeSpan outPoint,
        bool hlsRealtime,
        Option<int> targetFramerate)
    {
        var result = new FFmpegPlaybackSettings
        {
            FormatFlags = streamingMode switch
            {
                StreamingMode.HttpLiveStreamingSegmenter => SegmenterFormatFlags,
                _ => CommonFormatFlags
            },
            RealtimeOutput = streamingMode switch
            {
                StreamingMode.HttpLiveStreamingSegmenter => hlsRealtime,
                _ => true
            }
        };

        // always use one thread with realtime output
        result.ThreadCount = result.RealtimeOutput ? 1 : ffmpegProfile.ThreadCount;

        if (now != start || inPoint != TimeSpan.Zero)
        {
            result.StreamSeek = now - start + inPoint;
        }

        switch (streamingMode)
        {
            case StreamingMode.HttpLiveStreamingDirect:
                result.AudioFormat = FFmpegProfileAudioFormat.Copy;
                result.VideoFormat = FFmpegProfileVideoFormat.Copy;
                result.Deinterlace = false;
                break;
            case StreamingMode.TransportStreamHybrid:
            case StreamingMode.HttpLiveStreamingSegmenter:
            case StreamingMode.TransportStream:
                result.HardwareAcceleration = ffmpegProfile.HardwareAcceleration;

                if (NeedToScale(ffmpegProfile, videoVersion))
                {
                    IDisplaySize scaledSize = CalculateScaledSize(ffmpegProfile, videoVersion);
                    if (!scaledSize.IsSameSizeAs(videoVersion))
                    {
                        int fixedHeight = scaledSize.Height + scaledSize.Height % 2;
                        int fixedWidth = scaledSize.Width + scaledSize.Width % 2;
                        result.ScaledSize = Some((IDisplaySize)new DisplaySize(fixedWidth, fixedHeight));
                    }
                }

                IDisplaySize sizeAfterScaling = result.ScaledSize.IfNone(videoVersion);
                if (!sizeAfterScaling.IsSameSizeAs(ffmpegProfile.Resolution))
                {
                    result.PadToDesiredResolution = true;
                }

                if (ffmpegProfile.NormalizeFramerate)
                {
                    result.FrameRate = targetFramerate;
                }

                result.VideoTrackTimeScale = 90000;

                foreach (MediaStream stream in videoStream.Where(s => s.AttachedPic == false))
                {
                    result.VideoFormat = ffmpegProfile.VideoFormat;
                    result.VideoBitrate = ffmpegProfile.VideoBitrate;
                    result.VideoBufferSize = ffmpegProfile.VideoBufferSize;

                    result.VideoDecoder =
                        (result.HardwareAcceleration, stream.Codec, stream.PixelFormat) switch
                        {
                            (HardwareAccelerationKind.Nvenc, "h264", "yuv420p10le" or "yuv444p" or "yuv444p10le"
                                ) =>
                                "h264",
                            (HardwareAccelerationKind.Nvenc, "hevc", "yuv444p" or "yuv444p10le") => "hevc",
                            (HardwareAccelerationKind.Nvenc, "h264", _) => "h264_cuvid",
                            (HardwareAccelerationKind.Nvenc, "hevc", _) => "hevc_cuvid",
                            (HardwareAccelerationKind.Nvenc, "mpeg2video", _) => "mpeg2_cuvid",
                            (HardwareAccelerationKind.Nvenc, "mpeg4", _) => "mpeg4_cuvid",
                            (HardwareAccelerationKind.Qsv, "h264", _) => "h264_qsv",
                            (HardwareAccelerationKind.Qsv, "hevc", _) => "hevc_qsv",
                            (HardwareAccelerationKind.Qsv, "mpeg2video", _) => "mpeg2_qsv",

                            // temp disable mpeg4 hardware decoding for all vaapi
                            // TODO: check for codec support
                            (HardwareAccelerationKind.Vaapi, "mpeg4", _) => "mpeg4",

                            _ => null
                        };
                }

                result.PixelFormat = ffmpegProfile.BitDepth switch
                {
                    FFmpegProfileBitDepth.TenBit when ffmpegProfile.VideoFormat != FFmpegProfileVideoFormat.Mpeg2Video
                        => new PixelFormatYuv420P10Le(),
                    _ => new PixelFormatYuv420P()
                };

                result.AudioFormat = ffmpegProfile.AudioFormat;
                result.AudioBitrate = ffmpegProfile.AudioBitrate;
                result.AudioBufferSize = ffmpegProfile.AudioBufferSize;

                foreach (MediaStream _ in audioStream)
                {
                    // this can be optimized out later, depending on the audio codec
                    result.AudioChannels = ffmpegProfile.AudioChannels;
                }

                result.AudioSampleRate = ffmpegProfile.AudioSampleRate;
                result.AudioDuration = outPoint - inPoint;
                result.NormalizeLoudness = ffmpegProfile.NormalizeLoudness;

                result.Deinterlace = ffmpegProfile.DeinterlaceVideo == true &&
                                     videoVersion.VideoScanKind == VideoScanKind.Interlaced;

                break;
        }

        return result;
    }

    public FFmpegPlaybackSettings CalculateErrorSettings(
        StreamingMode streamingMode,
        FFmpegProfile ffmpegProfile,
        bool hlsRealtime) =>
        new()
        {
            // HardwareAcceleration = ffmpegProfile.HardwareAcceleration,
            HardwareAcceleration = HardwareAccelerationKind.None,
            FormatFlags = CommonFormatFlags,
            VideoFormat = ffmpegProfile.VideoFormat,
            VideoBitrate = ffmpegProfile.VideoBitrate,
            VideoBufferSize = ffmpegProfile.VideoBufferSize,
            AudioFormat = ffmpegProfile.AudioFormat,
            AudioBitrate = ffmpegProfile.AudioBitrate,
            AudioBufferSize = ffmpegProfile.AudioBufferSize,
            AudioChannels = ffmpegProfile.AudioChannels,
            AudioSampleRate = ffmpegProfile.AudioSampleRate,
            RealtimeOutput = streamingMode switch
            {
                StreamingMode.HttpLiveStreamingSegmenter => hlsRealtime,
                _ => true
            },
            VideoTrackTimeScale = 90000,
            FrameRate = 24
        };

    private static bool NeedToScale(FFmpegProfile ffmpegProfile, MediaVersion version) =>
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
