namespace ErsatzTV.FFmpeg.Option;

public class AudioSampleRateOutputOption : OutputOption
{
    private readonly int _sampleRate;

    public AudioSampleRateOutputOption(int sampleRate)
    {
        _sampleRate = sampleRate;
    }

    public override IList<string> OutputOptions => new List<string> { "-ar", $"{_sampleRate}k" };
}
