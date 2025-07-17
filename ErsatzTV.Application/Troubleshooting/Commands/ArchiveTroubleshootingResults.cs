namespace ErsatzTV.Application.Troubleshooting;

public record ArchiveTroubleshootingResults(int MediaItemId, int FFmpegProfileId, int WatermarkId)
    : IRequest<Option<string>>;
