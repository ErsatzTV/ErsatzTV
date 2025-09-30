using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.Troubleshooting;

public record ArchiveTroubleshootingResults(
    int MediaItemId,
    int FFmpegProfileId,
    StreamingMode StreamingMode,
    List<int> WatermarkIds,
    List<int> GraphicsElementIds,
    Option<int> SeekSeconds)
    : IRequest<Option<string>>;
