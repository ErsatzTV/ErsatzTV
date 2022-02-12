namespace ErsatzTV.FFmpeg.Option;

public class AudioBitrateOutputOption : OutputOption
{
    private readonly int _averageBitrate;
    private readonly int _maximumTolerance;
    private readonly int _decoderBufferSize;

    public AudioBitrateOutputOption(
        int averageBitrate,
        int maximumTolerance,
        int decoderBufferSize)
    {
        _averageBitrate = averageBitrate;
        _maximumTolerance = maximumTolerance;
        _decoderBufferSize = decoderBufferSize;
    }

    public override IList<string> OutputOptions => new List<string>
    {
        "-b:a", $"{_averageBitrate}k",
        "-maxrate:a", $"{_maximumTolerance}k",
        "-bufsize:a", $"{_decoderBufferSize}k"
    };
}
