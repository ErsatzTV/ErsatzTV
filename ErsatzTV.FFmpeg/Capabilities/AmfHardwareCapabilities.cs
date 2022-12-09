using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Capabilities;

public class AmfHardwareCapabilities : IHardwareCapabilities
{
    public bool CanDecode(string videoFormat, Option<IPixelFormat> maybePixelFormat) => false;

    public bool CanEncode(string videoFormat, Option<IPixelFormat> maybePixelFormat)
    {
        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        return (videoFormat, bitDepth) switch
        {
            // 10-bit hevc encoding is not yet supported by ffmpeg
            (VideoFormat.Hevc, 10) => false,

            // 10-bit h264 encoding is not support by any hardware
            (VideoFormat.H264, 10) => false,

            _ => true
        };
    }
}
