namespace ErsatzTV.FFmpeg.Option.HardwareAcceleration;

public class AmfHardwareAccelerationOption : GlobalOption
{
    public override IList<string> GlobalOptions => new List<string> { "-hwaccel", "dxva2" };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        FrameDataLocation = FrameDataLocation.Software
    };
}
