namespace ErsatzTV.FFmpeg.Decoder.Cuvid;

public class DecoderMpeg2Cuvid : IDecoder
{
    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Hardware;
    public IList<string> InputOptions => new List<string> { "-c:v", Name };
    public IList<string> OutputOptions => new List<string>();
    public FrameState NextState(FrameState currentState) => currentState;

    public string Name => "mpeg2_cuvid";
}
