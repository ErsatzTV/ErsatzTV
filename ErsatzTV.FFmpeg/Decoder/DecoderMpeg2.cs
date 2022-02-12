namespace ErsatzTV.FFmpeg.Decoder;

public class DecoderMpeg2Video : IDecoder
{
    public string Name => "mpeg2video";
    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Software;
    public IList<string> InputOptions => new List<string> { "-c:v", Name };
    public IList<string> OutputOptions => Array.Empty<string>();

    public FrameState NextState(FrameState currentState) =>
        currentState with { FrameDataLocation = OutputFrameDataLocation };
}
