namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class OverlaySubtitleCudaFilter : BaseFilter
{
    public override string Filter => "overlay_cuda";
    public override FrameState NextState(FrameState currentState) => currentState;
}
