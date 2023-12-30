namespace ErsatzTV.FFmpeg.OutputOption;

public class VideoBufferSizeOutputOption : OutputOption
{
    private readonly int _decoderBufferSize;

    public VideoBufferSizeOutputOption(int decoderBufferSize) => _decoderBufferSize = decoderBufferSize;

    public override string[] OutputOptions => new[]
    {
        "-bufsize:v", $"{_decoderBufferSize}k"
    };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoBufferSize = _decoderBufferSize
    };
}
