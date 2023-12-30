namespace ErsatzTV.FFmpeg.OutputOption;

public class AudioBitrateOutputOption : OutputOption
{
    private readonly int _bitrate;

    public AudioBitrateOutputOption(int bitrate) => _bitrate = bitrate;

    public override string[] OutputOptions => new[]
    {
        "-b:a", $"{_bitrate}k",
        "-maxrate:a", $"{_bitrate}k"
    };
}
