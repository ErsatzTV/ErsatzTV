using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Capabilities;

public interface IHardwareCapabilities
{
    public bool CanDecode(string videoFormat, Option<string> videoProfile, Option<IPixelFormat> maybePixelFormat);
    public bool CanEncode(string videoFormat, Option<string> videoProfile, Option<IPixelFormat> maybePixelFormat);
}
