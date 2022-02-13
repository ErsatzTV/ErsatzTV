namespace ErsatzTV.FFmpeg.Filter;

public class NormalizeLoudnessFilter : IPipelineFilterStep
{
    public StreamKind StreamKind => StreamKind.Audio;

    public string Filter => "loudnorm=I=-16:TP=-1.5:LRA=11";

    public FrameState NextState(FrameState currentState) => currentState with { NormalizeLoudness = true };
}
