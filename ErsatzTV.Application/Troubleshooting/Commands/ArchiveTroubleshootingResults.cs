namespace ErsatzTV.Application.Troubleshooting;

public record ArchiveTroubleshootingResults(
    int MediaItemId,
    int FFmpegProfileId,
    List<int> WatermarkIds,
    List<int> GraphicsElementIds,
    Option<int> SeekSeconds)
    : IRequest<Option<string>>;
