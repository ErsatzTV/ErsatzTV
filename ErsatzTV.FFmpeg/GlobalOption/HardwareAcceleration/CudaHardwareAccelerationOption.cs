namespace ErsatzTV.FFmpeg.GlobalOption.HardwareAcceleration;

public class CudaHardwareAccelerationOption : GlobalOption
{
    public override string[] GlobalOptions => new[] { "-hwaccel", "cuda" };
}
