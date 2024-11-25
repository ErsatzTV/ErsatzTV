using ErsatzTV.FFmpeg.Capabilities;

namespace ErsatzTV.FFmpeg.GlobalOption.HardwareAcceleration;

public class VaapiHardwareAccelerationOption : GlobalOption
{
    private readonly FFmpegCapability _decodeCapability;
    private readonly Option<string> _vaapiDevice;

    public VaapiHardwareAccelerationOption(Option<string> vaapiDevice, FFmpegCapability decodeCapability)
    {
        _vaapiDevice = vaapiDevice;
        _decodeCapability = decodeCapability;
    }

    public override string[] GlobalOptions
    {
        get
        {
            foreach (string vaapiDevice in _vaapiDevice)
            {
                return _decodeCapability == FFmpegCapability.Hardware
                    ? ["-hwaccel", "vaapi", "-vaapi_device", vaapiDevice]
                    : ["-vaapi_device", vaapiDevice];
            }

            return [ "-hwaccel", "vaapi" ];
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
