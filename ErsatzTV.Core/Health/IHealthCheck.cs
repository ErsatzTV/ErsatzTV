namespace ErsatzTV.Core.Health;

public interface IHealthCheck
{
    Task<HealthCheckResult> Check();
}