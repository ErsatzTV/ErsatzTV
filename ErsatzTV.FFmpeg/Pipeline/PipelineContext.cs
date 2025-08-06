namespace ErsatzTV.FFmpeg.Pipeline;

public record PipelineContext(
    HardwareAccelerationMode HardwareAccelerationMode,
    bool HasGraphicsEngine,
    bool HasWatermark,
    bool HasSubtitleOverlay,
    bool HasSubtitleText,
    bool ShouldDeinterlace,
    bool Is10BitOutput,
    bool IsIntelVaapiOrQsv,
    bool IsHdr);
