namespace ErsatzTV.FFmpeg;

public interface IPipelineFilterStep : IPipelineStep
{
    string Filter { get; }
}
