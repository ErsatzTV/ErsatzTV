namespace ErsatzTV.FFmpeg.Option;

public abstract class GlobalOption : IPipelineStep
{
    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Unknown;
    public abstract IList<string> GlobalOptions { get; }
    public IList<string> InputOptions => Array.Empty<string>();
    public IList<string> OutputOptions => Array.Empty<string>();
    public virtual FrameState NextState(FrameState currentState) => currentState;
}
