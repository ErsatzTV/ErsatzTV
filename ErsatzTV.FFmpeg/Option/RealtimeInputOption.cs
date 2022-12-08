using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Option;

public class RealtimeInputOption : IInputOption
{
    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();

    public IList<string> GlobalOptions => Array.Empty<string>();

    public IList<string> InputOptions(InputFile inputFile) => new List<string> { "-re" };

    public IList<string> FilterOptions => Array.Empty<string>();
    public IList<string> OutputOptions => Array.Empty<string>();
    public FrameState NextState(FrameState currentState) => currentState with { Realtime = true };

    public bool AppliesTo(AudioInputFile audioInputFile) => true;

    // don't use realtime input for a still image
    public bool AppliesTo(VideoInputFile videoInputFile) => videoInputFile.VideoStreams.All(s => !s.StillImage);

    public bool AppliesTo(ConcatInputFile concatInputFile) => true;
}
