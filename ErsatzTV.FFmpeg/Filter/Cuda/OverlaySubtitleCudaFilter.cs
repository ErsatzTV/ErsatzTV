namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class OverlaySubtitleCudaFilter : BaseFilter
{
    public override FrameState NextState(FrameState currentState) => currentState;

    public override string Filter => "overlay_cuda";
}
