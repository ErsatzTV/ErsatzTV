using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg;

public interface IPipelineStep
{
    IList<EnvironmentVariable> EnvironmentVariables { get; }
    IList<string> GlobalOptions { get; }
    IList<string> FilterOptions { get; }
    IList<string> OutputOptions { get; }
    IList<string> InputOptions(InputFile inputFile);

    FrameState NextState(FrameState currentState);
}
