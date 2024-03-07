namespace ErsatzTV.FFmpeg.Filter;

public class NormalizeLoudnessFilter : BaseFilter
{
    private readonly Option<int> _audioSampleRate;
    private readonly AudioFilter _loudnessFilter;

    public NormalizeLoudnessFilter(AudioFilter loudnessFilter, Option<int> audioSampleRate)
    {
        _loudnessFilter = loudnessFilter;
        _audioSampleRate = audioSampleRate;
    }

    public override string Filter
    {
        get
        {
            int audioSampleRate = _audioSampleRate.IfNone(48) * 1000;

            return _loudnessFilter switch
            {
                AudioFilter.LoudNorm => $"loudnorm=I=-16:TP=-1.5:LRA=11,aresample={audioSampleRate}",
                _ => string.Empty
            };
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState;
}
