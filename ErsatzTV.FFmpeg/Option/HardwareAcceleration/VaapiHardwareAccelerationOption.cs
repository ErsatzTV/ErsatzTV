namespace ErsatzTV.FFmpeg.Option.HardwareAcceleration;

public class VaapiHardwareAccelerationOption : GlobalOption
{
    private readonly string _vaapiDevice;

    public VaapiHardwareAccelerationOption(string vaapiDevice)
    {
        _vaapiDevice = vaapiDevice;
    }

    public override IList<string> GlobalOptions => new List<string>
        { "-hwaccel", "vaapi", "-vaapi_device", _vaapiDevice, "-hwaccel_output_format", "vaapi" };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
