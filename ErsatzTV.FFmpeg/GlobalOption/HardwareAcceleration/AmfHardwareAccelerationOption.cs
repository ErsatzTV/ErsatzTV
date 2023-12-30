namespace ErsatzTV.FFmpeg.GlobalOption.HardwareAcceleration;

public class AmfHardwareAccelerationOption : GlobalOption
{
    public override string[] GlobalOptions => new[] { "-hwaccel", "dxva2" };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        FrameDataLocation = FrameDataLocation.Software
    };
}
