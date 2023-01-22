using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Maintenance;

public class ReleaseMemoryHandler : IRequestHandler<ReleaseMemory, Unit>
{
    private readonly IFFmpegSegmenterService _ffmpegSegmenterService;
    private readonly ILogger<ReleaseMemoryHandler> _logger;

    public ReleaseMemoryHandler(
        IFFmpegSegmenterService ffmpegSegmenterService,
        ILogger<ReleaseMemoryHandler> logger)
    {
        _ffmpegSegmenterService = ffmpegSegmenterService;
        _logger = logger;
    }

    public Task<Unit> Handle(ReleaseMemory request, CancellationToken cancellationToken)
    {
        bool hasActiveWorkers = _ffmpegSegmenterService.SessionWorkers.Any() || FFmpegProcess.ProcessCount > 0;
        if (request.ForceAggressive || !hasActiveWorkers)
        {
            _logger.LogDebug("Starting aggressive garbage collection");
            GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        }
        else
        {
            _logger.LogDebug("Starting garbage collection");
            GC.Collect(2, GCCollectionMode.Forced, blocking: false);
        }

        GC.WaitForPendingFinalizers();
        GC.Collect();

        _logger.LogDebug("Completed garbage collection");

        return Task.FromResult(Unit.Default);
    }
}
