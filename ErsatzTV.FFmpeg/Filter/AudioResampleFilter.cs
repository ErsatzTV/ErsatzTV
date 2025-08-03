namespace ErsatzTV.FFmpeg.Filter;

public class AudioResampleFilter : BaseFilter
{
    public override string Filter => "aresample=async=1";

    public override FrameState NextState(FrameState currentState) => currentState;
}
