namespace ErsatzTV.FFmpeg.Pipeline;

public record PipelineContext(
    HardwareAccelerationMode HardwareAccelerationMode,
    bool HasWatermark,
    bool HasSubtitleOverlay,
    bool HasSubtitleText,
    bool ShouldDeinterlace,
    bool Is10BitOutput,
    bool IsIntelVaapiOrQsv);
