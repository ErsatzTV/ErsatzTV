namespace ErsatzTV.FFmpeg;

public record FFmpegPipeline(
    IList<IPipelineStep> PipelineSteps,
    IList<IPipelineFilterStep> VideoFilterSteps,
    IList<IPipelineFilterStep> AudioFilterSteps);
