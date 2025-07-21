namespace ErsatzTV.Application.Troubleshooting;

public record ArchiveTroubleshootingResults(
    int MediaItemId,
    int FFmpegProfileId,
    int WatermarkId,
    bool StartFromBeginning)
    : IRequest<Option<string>>;
