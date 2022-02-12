namespace ErsatzTV.FFmpeg.Option;

public class AudioChannelsOutputOption : OutputOption
{
    private readonly int _channels;

    public AudioChannelsOutputOption(int channels)
    {
        _channels = channels;
    }

    public override IList<string> OutputOptions => new List<string> { "-ac", _channels.ToString() };
}
