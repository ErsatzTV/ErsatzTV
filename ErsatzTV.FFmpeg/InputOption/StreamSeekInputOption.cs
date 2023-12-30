using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.InputOption;

public class StreamSeekInputOption : IInputOption
{
    private readonly TimeSpan _start;

    public StreamSeekInputOption(TimeSpan start) => _start = start;

    public EnvironmentVariable[] EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public string[] GlobalOptions => Array.Empty<string>();
    public string[] InputOptions(InputFile inputFile) => new[] { "-ss", $"{_start:c}" };
    public string[] FilterOptions => Array.Empty<string>();
    public string[] OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState;

    public bool AppliesTo(AudioInputFile audioInputFile) => true;

    // don't seek into a still image
    public bool AppliesTo(VideoInputFile videoInputFile) => videoInputFile.VideoStreams.All(s => !s.StillImage);

    // never seek when concatenating
    public bool AppliesTo(ConcatInputFile concatInputFile) => false;
}
