using ErsatzTV.Application.MediaItems;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Application.Troubleshooting;

public record StartTroubleshootingPlayback(
    Guid SessionId,
    PlayoutItemResult PlayoutItemResult,
    MediaItemInfo MediaItemInfo,
    TroubleshootingInfo TroubleshootingInfo) : IRequest, IFFmpegWorkerRequest;
