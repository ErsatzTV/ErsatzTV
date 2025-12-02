using ErsatzTV.FFmpeg.Capabilities.Qsv;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Capabilities;

public class QsvHardwareCapabilities(Option<VaapiHardwareCapabilities> vaapiHardwareCapabilities, QsvInitMode initMode)
    : IHardwareCapabilities
{
    public QsvInitMode InitMode { get; } = initMode;

    public FFmpegCapability CanDecode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat,
        bool isHdr)
    {
        foreach (var vaapi in vaapiHardwareCapabilities)
        {
            return vaapi.CanDecode(videoFormat, videoProfile, maybePixelFormat, isHdr);
        }

        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        return (videoFormat, bitDepth) switch
        {
            // 10-bit h264 decoding is likely not support by any hardware
            (VideoFormat.H264, 10) => FFmpegCapability.Software,

            _ => FFmpegCapability.Hardware
        };
    }

    public FFmpegCapability CanEncode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat)
    {
        foreach (var vaapi in vaapiHardwareCapabilities)
        {
            return vaapi.CanEncode(videoFormat, videoProfile, maybePixelFormat);
        }

        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        return (videoFormat, bitDepth) switch
        {
            // 10-bit h264 encoding is not support by any hardware
            (VideoFormat.H264, 10) => FFmpegCapability.Software,

            _ => FFmpegCapability.Hardware
        };
    }

    public Option<RateControlMode> GetRateControlMode(string videoFormat, Option<IPixelFormat> maybePixelFormat) =>
        Option<RateControlMode>.None;
}
