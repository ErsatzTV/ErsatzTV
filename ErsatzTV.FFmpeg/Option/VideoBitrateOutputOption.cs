namespace ErsatzTV.FFmpeg.Option;

public class VideoBitrateOutputOption : OutputOption
{
    private readonly int _bitrate;

    public VideoBitrateOutputOption(int bitrate) => _bitrate = bitrate;

    public override IList<string> OutputOptions => new List<string>
    {
        "-b:v", $"{_bitrate}k",
        "-maxrate:v", $"{_bitrate}k"
    };

    public override FrameState NextState(FrameState currentState) => currentState with
    {
        VideoBitrate = _bitrate
    };
}
