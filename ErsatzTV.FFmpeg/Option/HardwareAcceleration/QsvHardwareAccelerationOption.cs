namespace ErsatzTV.FFmpeg.Option.HardwareAcceleration;

public class QsvHardwareAccelerationOption : GlobalOption
{
    public override IList<string> GlobalOptions => new List<string>
    {
        "-hwaccel", "qsv",
        "-init_hw_device", "qsv=qsv:MFX_IMPL_hw_any"
    };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        HardwareAccelerationMode = HardwareAccelerationMode.Qsv
    };
}
