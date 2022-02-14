namespace ErsatzTV.FFmpeg.Encoder;

public abstract class EncoderBase : IEncoder
{
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> InputOptions => Array.Empty<string>();
    public IList<string> FilterOptions => Array.Empty<string>();
    public virtual IList<string> OutputOptions => new List<string> { Kind == StreamKind.Video ? "-c:v" : "-c:a", Name };
    public abstract FrameState NextState(FrameState currentState);

    public abstract string Name { get; }
    public abstract StreamKind Kind { get; }
    public virtual string Filter => string.Empty;
}
