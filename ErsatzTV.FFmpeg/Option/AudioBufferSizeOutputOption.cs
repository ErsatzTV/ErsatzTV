namespace ErsatzTV.FFmpeg.Option;

public class AudioBufferSizeOutputOption : OutputOption
{
    private readonly int _decoderBufferSize;

    public AudioBufferSizeOutputOption(int decoderBufferSize) => _decoderBufferSize = decoderBufferSize;

    public override IList<string> OutputOptions => new List<string>
    {
        "-bufsize:a", $"{_decoderBufferSize}k"
    };
}
