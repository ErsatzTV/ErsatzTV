using CliWrap;

namespace ErsatzTV.Application.Troubleshooting;

public record StartTroubleshootingPlayback(Command Command) : IRequest, IFFmpegWorkerRequest;
