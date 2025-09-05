namespace ErsatzTV.FFmpeg.GlobalOption.HardwareAcceleration;

public class RkmppHardwareAccelerationOption : GlobalOption
{
    public override string[] GlobalOptions => new[] { "-hwaccel", "rkmpp" };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        FrameDataLocation = FrameDataLocation.Hardware
    };
}
