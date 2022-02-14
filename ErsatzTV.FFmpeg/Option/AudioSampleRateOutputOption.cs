namespace ErsatzTV.FFmpeg.Option;

public class AudioSampleRateOutputOption : OutputOption
{
    private readonly int _sampleRate;

    public AudioSampleRateOutputOption(int sampleRate)
    {
        _sampleRate = sampleRate;
    }

    public override IList<string> OutputOptions => new List<string> { "-ar", $"{_sampleRate}k" };

    public override FrameState NextState(FrameState currentState) =>
        currentState with { AudioSampleRate = _sampleRate };
}
