namespace ErsatzTV.FFmpeg.Option;

public class CudaHardwareAccelerationOption : GlobalOption
{
    public override IList<string> GlobalOptions => new List<string> { "-hwaccel", "cuda" };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        HardwareAccelerationMode = HardwareAccelerationMode.Nvenc
    };
}
