using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Capabilities;

public class NoHardwareCapabilities : IHardwareCapabilities
{
    public FFmpegCapability CanDecode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat) =>
        FFmpegCapability.Software;

    public FFmpegCapability CanEncode(
        string videoFormat,
        Option<string> videoProfile,
        Option<IPixelFormat> maybePixelFormat) =>
        FFmpegCapability.Software;
}
