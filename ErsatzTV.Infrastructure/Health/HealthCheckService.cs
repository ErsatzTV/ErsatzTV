using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Infrastructure.Health;

public class HealthCheckService : IHealthCheckService
{
    private const string CacheKey = "healthcheck.summary";

    private readonly List<IHealthCheck> _checks; // ReSharper disable SuggestBaseTypeForParameterInConstructor
    private readonly IMemoryCache _memoryCache;
    private readonly IMediator _mediator;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(
        IMacOsConfigFolderHealthCheck macOsConfigFolderHealthCheck,
        IFFmpegVersionHealthCheck ffmpegVersionHealthCheck,
        IFFmpegReportsHealthCheck ffmpegReportsHealthCheck,
        IHardwareAccelerationHealthCheck hardwareAccelerationHealthCheck,
        IMovieMetadataHealthCheck movieMetadataHealthCheck,
        IEpisodeMetadataHealthCheck episodeMetadataHealthCheck,
        IZeroDurationHealthCheck zeroDurationHealthCheck,
        IFileNotFoundHealthCheck fileNotFoundHealthCheck,
        IUnavailableHealthCheck unavailableHealthCheck,
        IVaapiDriverHealthCheck vaapiDriverHealthCheck,
        IErrorReportsHealthCheck errorReportsHealthCheck,
        IUnifiedDockerHealthCheck unifiedDockerHealthCheck,
        IDowngradeHealthCheck downgradeHealthCheck,
        IEmptyScheduleHealthCheck emptyScheduleHealthCheck,
        IMemoryCache memoryCache,
        IMediator mediator,
        ILogger<HealthCheckService> logger)
    {
        _memoryCache = memoryCache;
        _mediator = mediator;
        _logger = logger;
        _checks =
        [
            downgradeHealthCheck,
            macOsConfigFolderHealthCheck,
            unifiedDockerHealthCheck,
            ffmpegVersionHealthCheck,
            ffmpegReportsHealthCheck,
            hardwareAccelerationHealthCheck,
            movieMetadataHealthCheck,
            episodeMetadataHealthCheck,
            zeroDurationHealthCheck,
            fileNotFoundHealthCheck,
            unavailableHealthCheck,
            emptyScheduleHealthCheck,
            vaapiDriverHealthCheck,
            errorReportsHealthCheck
        ];
    }

    public async Task<List<HealthCheckResult>> PerformHealthChecks(CancellationToken cancellationToken)
    {
        List<HealthCheckResult> result = await _checks.Map(c =>
            {
                var failedResult = new HealthCheckResult(
                    c.Title,
                    HealthCheckStatus.Fail,
                    "Health check failure; see logs",
                    "Health check failure",
                    None);
                return TryAsync(() => c.Check(cancellationToken)).IfFail(ex => LogAndReturn(ex, failedResult));
            })
            .SequenceParallel()
            .Map(results => results.ToList());

        var summary = new HealthCheckSummary(
            result.Count(x => x.Status is HealthCheckStatus.Warning),
            result.Count(x => x.Status is HealthCheckStatus.Fail));

        _memoryCache.Set(CacheKey, summary);

        await _mediator.Publish(summary, cancellationToken);

        return result;
    }

    public HealthCheckSummary GetHealthCheckSummary() =>
        _memoryCache.Get<HealthCheckSummary>(CacheKey) ?? new HealthCheckSummary(0, 0);

    private HealthCheckResult LogAndReturn(Exception ex, HealthCheckResult failedResult)
    {
        if (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to run health check {Title}", failedResult.Title);
        }

        return failedResult;
    }
}
