using CliWrap;
using ErsatzTV.Application.MediaItems;

namespace ErsatzTV.Application.Troubleshooting;

public record StartTroubleshootingPlayback(
    Command Command,
    MediaItemInfo MediaItemInfo,
    TroubleshootingInfo TroubleshootingInfo) : IRequest, IFFmpegWorkerRequest;
