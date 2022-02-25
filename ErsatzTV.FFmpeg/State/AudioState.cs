using LanguageExt;

namespace ErsatzTV.FFmpeg.State;

public record AudioState(
    Option<TimeSpan> Start,
    Option<string> AudioFormat,
    Option<int> AudioChannels,
    Option<int> AudioBitrate,
    Option<int> AudioBufferSize,
    Option<int> AudioSampleRate,
    Option<TimeSpan> AudioDuration,
    bool NormalizeLoudness);
