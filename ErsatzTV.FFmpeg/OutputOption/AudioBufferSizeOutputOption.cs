namespace ErsatzTV.FFmpeg.OutputOption;

public class AudioBufferSizeOutputOption : OutputOption
{
    private readonly int _decoderBufferSize;

    public AudioBufferSizeOutputOption(int decoderBufferSize) => _decoderBufferSize = decoderBufferSize;

    public override string[] OutputOptions => new[]
    {
        "-bufsize:a", $"{_decoderBufferSize}k"
    };
}
