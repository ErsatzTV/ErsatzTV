using ErsatzTV.FFmpeg.Capabilities.Vaapi;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.Capabilities;

public class VaapiHardwareCapabilities : IHardwareCapabilities
{
    private readonly List<VaapiProfileEntrypoint> _profileEntrypoints;

    public VaapiHardwareCapabilities(List<VaapiProfileEntrypoint> profileEntrypoints) =>
        _profileEntrypoints = profileEntrypoints;

    public bool CanDecode(string videoFormat, string videoProfile, Option<IPixelFormat> maybePixelFormat)
    {
        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        return (videoFormat, videoProfile) switch
        {
            // no hardware decoding of 10-bit h264
            (VideoFormat.H264, _) when bitDepth == 10 => false,

            _ => true
        };
    }

    public bool CanEncode(string videoFormat, string videoProfile, Option<IPixelFormat> maybePixelFormat)
    {
        int bitDepth = maybePixelFormat.Map(pf => pf.BitDepth).IfNone(8);

        return (videoFormat, videoProfile) switch
        {
            // vaapi cannot encode 10-bit h264
            (VideoFormat.H264, _) when bitDepth == 10 => false,

            _ => true
        };
    }
}
