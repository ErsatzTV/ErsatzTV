using System.Globalization;

namespace ErsatzTV.FFmpeg.Filter;

public class NormalizeLoudnessFilter(AudioFilter loudnessFilter, Option<double> targetLoudness, Option<int> sampleRate)
    : BaseFilter
{
    public override string Filter
    {
        get
        {
            double loudness = targetLoudness.IfNone(-16);
            int audioSampleRate = sampleRate.IfNone(48) * 1000;

            return loudnessFilter switch
            {
                AudioFilter.LoudNorm => $"loudnorm=I={loudness.ToString(NumberFormatInfo.InvariantInfo)}:TP=-1.5:LRA=11,aresample={audioSampleRate}",
                _ => string.Empty
            };
        }
    }

    public override FrameState NextState(FrameState currentState) => currentState;
}
