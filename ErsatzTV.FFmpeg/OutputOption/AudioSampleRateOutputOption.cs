namespace ErsatzTV.FFmpeg.OutputOption;

public class AudioSampleRateOutputOption : OutputOption
{
    private readonly int _sampleRate;

    public AudioSampleRateOutputOption(int sampleRate) => _sampleRate = sampleRate;

    public override string[] OutputOptions => new[] { "-ar", $"{_sampleRate}k" };
}
