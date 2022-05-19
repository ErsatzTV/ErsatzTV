namespace ErsatzTV.FFmpeg.Filter;

public class NormalizeLoudnessFilter : BaseFilter
{
    public override string Filter => "loudnorm=I=-16:TP=-1.5:LRA=11";

    public override FrameState NextState(FrameState currentState) => currentState;
}
