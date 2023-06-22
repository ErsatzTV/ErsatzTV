using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Capabilities;

public class DefaultHardwareCapabilities : IHardwareCapabilities
{
    public FFmpegCapability CanDecode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat)
    {
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
