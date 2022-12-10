namespace ErsatzTV.FFmpeg.Option.HardwareAcceleration;

public class VaapiHardwareAccelerationOption : GlobalOption
{
    private readonly string _vaapiDevice;
    private readonly bool _canDecode;

    public VaapiHardwareAccelerationOption(string vaapiDevice, bool canDecode)
    {
        _vaapiDevice = vaapiDevice;
        _canDecode = canDecode;
    }

    public override IList<string> GlobalOptions
    {
        get
        {
            var result = new List<string> { "-vaapi_device", _vaapiDevice };

            if (_canDecode)
            {
                result.InsertRange(0, new[] { "-hwaccel", "vaapi" });
            }

            return result;
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
