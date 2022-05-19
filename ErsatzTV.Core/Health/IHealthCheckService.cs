namespace ErsatzTV.Core.Health;

public interface IHealthCheckService
{
    Task<List<HealthCheckResult>> PerformHealthChecks(CancellationToken cancellationToken);
}
