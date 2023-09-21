namespace ErsatzTV.FFmpeg.State;

public record AudioState(
    Option<string> AudioFormat,
    Option<int> AudioChannels,
    Option<int> AudioBitrate,
    Option<int> AudioBufferSize,
    Option<int> AudioSampleRate,
    Option<TimeSpan> AudioDuration,
    AudioFilter NormalizeLoudnessFilter)
{
    public static readonly AudioState Copy = new(
        Format.AudioFormat.Copy,
        Option<int>.None,
        Option<int>.None,
        Option<int>.None,
        Option<int>.None,
        Option<TimeSpan>.None,
        AudioFilter.None
    );
}
