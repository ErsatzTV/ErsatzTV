using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Maintenance;

public class ReleaseMemoryHandler : IRequestHandler<ReleaseMemory, Unit>
{
    private static long _lastRelease;
    
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
        if (!request.ForceAggressive && _lastRelease > request.RequestTime.Ticks)
        {
            // we've already released since the request was created, so don't bother
            return Task.FromResult(Unit.Default);
        }

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
        Interlocked.Exchange(ref _lastRelease, DateTimeOffset.Now.Ticks);

        return Task.FromResult(Unit.Default);
    }
}
