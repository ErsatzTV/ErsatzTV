namespace ErsatzTV.FFmpeg;

public record FilterChain(
    List<IPipelineFilterStep> VideoFilterSteps,
    List<IPipelineFilterStep> WatermarkFilterSteps,
    List<IPipelineFilterStep> SubtitleFilterSteps,
    List<IPipelineFilterStep> GraphicsEngineFilterSteps,
    List<IPipelineFilterStep> WatermarkOverlayFilterSteps,
    List<IPipelineFilterStep> SubtitleOverlayFilterSteps,
    List<IPipelineFilterStep> GraphicsEngineOverlayFilterSteps,
    List<IPipelineFilterStep> PixelFormatFilterSteps)
{
    public static readonly FilterChain Empty = new([], [], [], [], [], [], [], []);
}
