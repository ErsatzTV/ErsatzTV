namespace ErsatzTV.FFmpeg.GlobalOption.HardwareAcceleration;

public class Dxva2HardwareAccelerationOption : GlobalOption
{
    public override string[] GlobalOptions => ["-hwaccel", "dxva2"];

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        FrameDataLocation = FrameDataLocation.Software
    };
}
