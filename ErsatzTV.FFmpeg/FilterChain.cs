namespace ErsatzTV.FFmpeg;

public record FilterChain(
    List<IPipelineFilterStep> VideoFilterSteps,
    List<IPipelineFilterStep> WatermarkFilterSteps,
    List<IPipelineFilterStep> SubtitleFilterSteps,
    List<IPipelineFilterStep> WatermarkOverlayFilterSteps,
    List<IPipelineFilterStep> SubtitleOverlayFilterSteps,
    List<IPipelineFilterStep> PixelFormatFilterSteps)
{
    public static readonly FilterChain Empty = new(
        new List<IPipelineFilterStep>(),
        new List<IPipelineFilterStep>(),
        new List<IPipelineFilterStep>(),
        new List<IPipelineFilterStep>(),
        new List<IPipelineFilterStep>(),
        new List<IPipelineFilterStep>());
}
