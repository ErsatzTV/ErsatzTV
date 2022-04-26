namespace ErsatzTV.Core.Health;

public interface IHealthCheck
{
    string Title { get; }
    Task<HealthCheckResult> Check(CancellationToken cancellationToken);
}
