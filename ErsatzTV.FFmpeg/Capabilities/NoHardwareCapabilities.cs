using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Capabilities;

public class NoHardwareCapabilities : IHardwareCapabilities
{
    public bool CanDecode(string videoFormat, Option<IPixelFormat> maybePixelFormat) => false;
    public bool CanEncode(string videoFormat, Option<IPixelFormat> maybePixelFormat) => false;
}
