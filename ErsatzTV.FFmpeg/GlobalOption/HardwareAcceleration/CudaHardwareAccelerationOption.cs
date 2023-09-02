namespace ErsatzTV.FFmpeg.GlobalOption.HardwareAcceleration;

public class CudaHardwareAccelerationOption : GlobalOption
{
    public override IList<string> GlobalOptions => new List<string> { "-hwaccel", "cuda" };
}
