namespace ErsatzTV.FFmpeg.Filter.Cuda;

public class OverlaySubtitleCudaFilter : BaseFilter
{
    public override string Filter => "overlay_cuda=x=(W-w)/2:y=(H-h)/2";
    public override FrameState NextState(FrameState currentState) => currentState;
}
