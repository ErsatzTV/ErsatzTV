using ErsatzTV.FFmpeg.Environment;

namespace ErsatzTV.FFmpeg.Option;

public abstract class OutputOption : IPipelineStep
{
    public FrameDataLocation OutputFrameDataLocation => FrameDataLocation.Unknown;
    public IList<EnvironmentVariable> EnvironmentVariables => Array.Empty<EnvironmentVariable>();
    public IList<string> GlobalOptions => Array.Empty<string>();
    public IList<string> VideoInputOptions(VideoInputFile videoInputFile) => Array.Empty<string>();
    public IList<string> FilterOptions => Array.Empty<string>();
    public abstract IList<string> OutputOptions { get; }

    public virtual FrameState NextState(FrameState currentState) => currentState;
}
