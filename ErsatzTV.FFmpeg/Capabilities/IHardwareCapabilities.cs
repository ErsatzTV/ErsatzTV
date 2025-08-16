using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Capabilities;

public interface IHardwareCapabilities
{
    FFmpegCapability CanDecode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat,
        bool isHdr);

    FFmpegCapability CanEncode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat);

    Option<RateControlMode> GetRateControlMode(
        string videoFormat,
        Option<IPixelFormat> maybePixelFormat);
}
