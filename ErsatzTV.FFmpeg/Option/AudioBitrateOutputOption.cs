namespace ErsatzTV.FFmpeg.Option;

public class AudioBitrateOutputOption : OutputOption
{
    private readonly int _bitrate;

    public AudioBitrateOutputOption(int bitrate) => _bitrate = bitrate;

    public override IList<string> OutputOptions => new List<string>
    {
        "-b:a", $"{_bitrate}k",
        "-maxrate:a", $"{_bitrate}k"
    };
}
