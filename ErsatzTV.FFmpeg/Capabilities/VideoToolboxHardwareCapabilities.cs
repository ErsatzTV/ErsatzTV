using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using ErsatzTV.FFmpeg.Capabilities.VideoToolbox;
using ErsatzTV.FFmpeg.Format;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.FFmpeg.Capabilities;

public class VideoToolboxHardwareCapabilities : IHardwareCapabilities
{
    private static readonly ConcurrentDictionary<string, bool> Encoders = new();
    private static readonly ConcurrentDictionary<string, bool> Decoders = new();

    private readonly IFFmpegCapabilities _ffmpegCapabilities;
    private readonly ILogger _logger;

    public VideoToolboxHardwareCapabilities(IFFmpegCapabilities ffmpegCapabilities, ILogger logger)
    {
        _ffmpegCapabilities = ffmpegCapabilities;
        _logger = logger;
    }

    public FFmpegCapability CanDecode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat,
        ColorParams colorParams)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && Decoders.IsEmpty)
        {
            if (VideoToolboxUtil.IsHardwareDecoderSupported(FourCC.Av1, _logger))
            {
                Decoders.AddOrUpdate(VideoFormat.Av1, true, (_, _) => true);
            }

            if (VideoToolboxUtil.IsHardwareDecoderSupported(FourCC.H264, _logger))
            {
                Decoders.AddOrUpdate(VideoFormat.H264, true, (_, _) => true);
            }

            if (VideoToolboxUtil.IsHardwareDecoderSupported(FourCC.Hevc, _logger))
            {
                Decoders.AddOrUpdate(VideoFormat.Hevc, true, (_, _) => true);
            }

            if (VideoToolboxUtil.IsHardwareDecoderSupported(FourCC.Mpeg2Video, _logger))
            {
                Decoders.AddOrUpdate(VideoFormat.Mpeg2Video, true, (_, _) => true);
            }

            if (VideoToolboxUtil.IsHardwareDecoderSupported(FourCC.Mpeg4, _logger))
            {
                Decoders.AddOrUpdate(VideoFormat.Mpeg4, true, (_, _) => true);
            }

            if (VideoToolboxUtil.IsHardwareDecoderSupported(FourCC.Vp9, _logger))
            {
                Decoders.AddOrUpdate(VideoFormat.Vp9, true, (_, _) => true);
            }
        }

        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);
        return (videoFormat, bitDepth) switch
        {
            // 10-bit h264 decoding is likely not support by any hardware
            (VideoFormat.H264, 10) => FFmpegCapability.Software,

            (_, _) when colorParams.IsBt2020Ten => FFmpegCapability.Software,

            _ => Decoders.ContainsKey(videoFormat) ? FFmpegCapability.Hardware : FFmpegCapability.Software
        };
    }

    public FFmpegCapability CanEncode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && Encoders.IsEmpty)
        {
            List<string> encoderList = VideoToolboxUtil.GetAvailableEncoders(_logger);
            _logger.LogDebug("VideoToolbox reports {Count} encoders", encoderList.Count);

            // we only really care about h264 and hevc hardware encoders
            foreach (string encoder in encoderList)
            {
                if (encoder.Contains("HEVC (HW)", StringComparison.OrdinalIgnoreCase))
                {
                    Encoders.AddOrUpdate(VideoFormat.Hevc, true, (_, _) => true);
                }

                if (encoder.Contains("H.264 (HW)", StringComparison.OrdinalIgnoreCase))
                {
                    Encoders.AddOrUpdate(VideoFormat.H264, true, (_, _) => true);
                }
            }
        }

        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);
        return (videoFormat, bitDepth) switch
        {
            // 10-bit h264 encoding is not support by any hardware
            (VideoFormat.H264, 10) => FFmpegCapability.Software,

            (VideoFormat.H264, 8) =>
                _ffmpegCapabilities.HasEncoder(FFmpegKnownEncoder.H264VideoToolbox) && Encoders.ContainsKey(videoFormat)
                    ? FFmpegCapability.Hardware
                    : FFmpegCapability.Software,

            (VideoFormat.Hevc, _) =>
                _ffmpegCapabilities.HasEncoder(FFmpegKnownEncoder.HevcVideoToolbox) && Encoders.ContainsKey(videoFormat)
                    ? FFmpegCapability.Hardware
                    : FFmpegCapability.Software,

            _ => FFmpegCapability.Software
        };
    }

    public Option<RateControlMode> GetRateControlMode(string videoFormat, Option<IPixelFormat> maybePixelFormat) =>
        Option<RateControlMode>.None;
}
