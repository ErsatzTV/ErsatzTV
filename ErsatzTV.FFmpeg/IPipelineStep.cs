using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg;

public interface IPipelineStep
{
    IList<EnvironmentVariable> EnvironmentVariables { get; }
    IList<string> GlobalOptions { get; }
    IList<string> InputOptions(InputFile inputFile);
    IList<string> FilterOptions { get; }
    IList<string> OutputOptions { get; }

    FrameState NextState(FrameState currentState);
}
