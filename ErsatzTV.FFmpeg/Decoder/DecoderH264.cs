namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderH264 : IDecoder
{
    public string Name => "h264";
    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
    public IList<string> InputOptions => new List<string> { "-c:v", Name };
    public IList<string> OutputOptions => Array.Empty<string>();

    public FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = OutputFrameDataLocation };
}
