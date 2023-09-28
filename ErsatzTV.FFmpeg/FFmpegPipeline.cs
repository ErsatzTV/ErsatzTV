namespace ErsatzTV.FFmpeg;

public record FFmpegPipeline(IList<IPipelineStep> PipelineSteps, bool IsIntelVaapiOrQsv);
