namespace ErsatzTV.FFmpeg.Encoder;

public class EncoderCopyAudio : IEncoder
{
    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
    public IList<string> InputOptions => Array.Empty<string>();
    public IList<string> OutputOptions => new List<string> { "-c:a", "copy" };
    public FrameState NextState(FrameState currentState) => currentState;
    public string Name => "copy";
    public StreamKind Kind => StreamKind.Video;
}
