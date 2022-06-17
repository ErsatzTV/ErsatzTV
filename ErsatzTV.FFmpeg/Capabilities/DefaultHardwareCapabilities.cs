using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Capabilities;

public class DefaultHardwareCapabilities : IHardwareCapabilities
{
    public bool CanDecode(string videoFormat, Option<IPixelFormat> maybePixelFormat) => true;
    public bool CanEncode(string videoFormat, Option<IPixelFormat> maybePixelFormat) => true;
}
