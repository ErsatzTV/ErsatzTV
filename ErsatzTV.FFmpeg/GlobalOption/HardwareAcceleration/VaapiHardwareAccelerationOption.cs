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

    public override IList<string> GlobalOptions
    {
        get
        {
            var result = new List<string> { "-vaapi_device", _vaapiDevice };

            if (_decodeCapability == FFmpegCapability.Hardware)
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
