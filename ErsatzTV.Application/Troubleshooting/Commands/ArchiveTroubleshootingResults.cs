namespace ErsatzTV.Application.Troubleshooting;

public record ArchiveTroubleshootingResults(
    int MediaItemId,
    int FFmpegProfileId,
    List<int> WatermarkIds,
    List<int> GraphicsElementIds,
    bool StartFromBeginning)
    : IRequest<Option<string>>;
