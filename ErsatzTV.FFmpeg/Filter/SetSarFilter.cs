namespace ErsatzTV.FFmpeg.Filter;

public class SetSarFilter : IPipelineFilterStep
{
    public StreamKind StreamKind => StreamKind.Video;
    public string Filter => "setsar=1";
    public FrameState NextState(FrameState currentState) => currentState;
}
