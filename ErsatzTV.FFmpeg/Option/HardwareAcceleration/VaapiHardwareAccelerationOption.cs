namespace ErsatzTV.FFmpeg.Option.HardwareAcceleration;

public class VaapiHardwareAccelerationOption : GlobalOption
{
    public override IList<string> GlobalOptions => new List<string>
        { "-hwaccel", "vaapi", "-hwaccel_output_format", "vaapi" };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        HardwareAccelerationMode = HardwareAccelerationMode.Vaapi,
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
