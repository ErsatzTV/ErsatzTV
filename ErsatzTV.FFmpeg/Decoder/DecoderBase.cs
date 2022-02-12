namespace ErsatzTV.FFmpeg.Decoder;

public abstract class DecoderBase : IDecoder
{
    public abstract FrameDataLocation OutputFrameDataLocation { get; }
    public IList<string> GlobalOptions => Array.Empty<string>();
    public virtual IList<string> InputOptions => new List<string> { "-c:v", Name };
    public IList<string> OutputOptions => Array.Empty<string>();
    public virtual FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = OutputFrameDataLocation };
    public abstract string Name { get; }
}
