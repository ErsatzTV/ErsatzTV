namespace ErsatzTV.FFmpeg.Option;

public class VideoBitrateOutputOption : OutputOption
{
    private readonly int _averageBitrate;
    private readonly int _maximumTolerance;
    private readonly int _decoderBufferSize;

    public VideoBitrateOutputOption(
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
        "-b:v", $"{_averageBitrate}k",
        "-maxrate:v", $"{_maximumTolerance}k",
        "-bufsize:v", $"{_decoderBufferSize}k"
    };
}
