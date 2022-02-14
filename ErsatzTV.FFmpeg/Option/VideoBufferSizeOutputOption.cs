namespace ErsatzTV.FFmpeg.Option;

public class VideoBufferSizeOutputOption : OutputOption
{
    private readonly int _decoderBufferSize;

    public VideoBufferSizeOutputOption(int decoderBufferSize)
    {
        _decoderBufferSize = decoderBufferSize;
    }

    public override IList<string> OutputOptions => new List<string>
    {
        "-bufsize:v", $"{_decoderBufferSize}k"
    };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoBufferSize = _decoderBufferSize
    };
}
