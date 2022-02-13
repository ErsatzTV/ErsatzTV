namespace ErsatzTV.FFmpeg.Option.HardwareAcceleration;

public class CudaHardwareAccelerationOption : GlobalOption
{
    public override IList<string> GlobalOptions => new List<string> { "-hwaccel", "cuda" };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        HardwareAccelerationMode = HardwareAccelerationMode.Nvenc
    };
}
