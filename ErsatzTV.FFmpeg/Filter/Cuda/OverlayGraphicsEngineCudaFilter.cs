namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class OverlayGraphicsEngineCudaFilter : BaseFilter
{
    public override string Filter => "overlay_cuda";

    public override FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = FrameDataLocation.Hardware };
}
