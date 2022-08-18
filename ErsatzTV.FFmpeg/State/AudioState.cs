namespace ErsatzTV.FFmpeg.State;

public record AudioState(
    Option<string> AudioFormat,
    Option<int> AudioChannels,
    Option<int> AudioBitrate,
    Option<int> AudioBufferSize,
    Option<int> AudioSampleRate,
    Option<TimeSpan> AudioDuration,
    bool NormalizeLoudness)
{
    public static readonly AudioState Copy = new(
        Format.AudioFormat.Copy,
        Option<int>.None,
        Option<int>.None,
        Option<int>.None,
        Option<int>.None,
        Option<TimeSpan>.None,
        false
    );
}
