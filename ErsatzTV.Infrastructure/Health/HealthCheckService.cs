using ErsatzTV.Core.Health;
using ErsatzTV.Core.Health.Checks;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Infrastructure.Health;

public class HealthCheckService : IHealthCheckService
{
    private readonly List<IHealthCheck> _checks; // ReSharper disable SuggestBaseTypeForParameterInConstructor
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(
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
        ILogger<HealthCheckService> logger)
    {
        _logger = logger;
        _checks = new List<IHealthCheck>
        {
            ffmpegVersionHealthCheck,
            ffmpegReportsHealthCheck,
            hardwareAccelerationHealthCheck,
            movieMetadataHealthCheck,
            episodeMetadataHealthCheck,
            zeroDurationHealthCheck,
            fileNotFoundHealthCheck,
            unavailableHealthCheck,
            vaapiDriverHealthCheck,
            errorReportsHealthCheck
        };
    }

    public Task<List<HealthCheckResult>> PerformHealthChecks(CancellationToken cancellationToken) =>
        _checks.Map(
                c =>
                {
                    var failedResult = new HealthCheckResult(
                        c.Title,
                        HealthCheckStatus.Fail,
                        "Health check failure; see logs",
                        None);
                    return TryAsync(() => c.Check(cancellationToken)).IfFail(ex => LogAndReturn(ex, failedResult));
                })
            .SequenceParallel()
            .Map(results => results.ToList());

    private HealthCheckResult LogAndReturn(Exception ex, HealthCheckResult failedResult)
    {
        if (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to run health check {Title}", failedResult.Title);
        }

        return failedResult;
    }
}
