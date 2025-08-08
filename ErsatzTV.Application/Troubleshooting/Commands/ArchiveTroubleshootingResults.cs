namespace ErsatzTV.Application.Troubleshooting;

public record ArchiveTroubleshootingResults(
    int MediaItemId,
    int FFmpegProfileId,
    List<int> WatermarkIds,
    bool StartFromBeginning)
    : IRequest<Option<string>>;
