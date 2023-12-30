using ErsatzTV.FFmpeg.Capabilities;

namespace ErsatzTV.FFmpeg.GlobalOption.HardwareAcceleration;

public class VaapiHardwareAccelerationOption : GlobalOption
{
    private readonly FFmpegCapability _decodeCapability;
    private readonly string _vaapiDevice;

    public VaapiHardwareAccelerationOption(string vaapiDevice, FFmpegCapability decodeCapability)
    {
        _vaapiDevice = vaapiDevice;
        _decodeCapability = decodeCapability;
    }

    public override string[] GlobalOptions => _decodeCapability == FFmpegCapability.Hardware
        ? new[] { "-hwaccel", "vaapi", "-vaapi_device", _vaapiDevice }
        : new[] { "-vaapi_device", _vaapiDevice };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
