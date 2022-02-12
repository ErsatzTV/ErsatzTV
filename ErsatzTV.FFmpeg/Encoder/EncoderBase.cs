namespace ErsatzTV.FFmpeg.Encoder;

public abstract class EncoderBase : IEncoder
{
    public abstract FrameDataLocation OutputFrameDataLocation { get; }
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions => Array.Empty<string>();
    public virtual IList<string> OutputOptions => new List<string> { "-c:v", Name };
    public abstract FrameState NextState(FrameState currentState);

    public abstract string Name { get; }
    public abstract StreamKind Kind { get; }
}
