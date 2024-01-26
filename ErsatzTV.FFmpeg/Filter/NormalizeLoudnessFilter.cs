namespace ErsatzTV.FFmpeg.Filter;

public class NormalizeLoudnessFilter : BaseFilter
{
    private readonly AudioFilter _loudnessFilter;

    public NormalizeLoudnessFilter(AudioFilter loudnessFilter) => _loudnessFilter = loudnessFilter;

    public override string Filter => _loudnessFilter switch
    {
        AudioFilter.LoudNorm => "loudnorm=I=-16:TP=-1.5:LRA=11",
        _ => string.Empty
    };

    public override FrameState NextState(FrameState currentState) => currentState;
}
