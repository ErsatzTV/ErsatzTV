using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Option;

public class StreamSeekInputOption : IPipelineStep
{
    private readonly TimeSpan _start;

    public StreamSeekInputOption(TimeSpan start)
    {
        _start = start;
    }

    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public IList<string> GlobalOptions => Array.Empty<string>();

    // don't seek into a still image
    public IList<string> VideoInputOptions(VideoInputFile videoInputFile) =>
        _start == TimeSpan.Zero || videoInputFile.Streams.Any(s => s.StillImage)
            ? Array.Empty<string>()
            : new List<string> { "-ss", $"{_start:c}" };

    public IList<string> FilterOptions => Array.Empty<string>();
    public IList<string> OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState with { Start = _start };
}
