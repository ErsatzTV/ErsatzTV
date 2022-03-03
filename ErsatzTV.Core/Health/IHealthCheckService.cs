using System.Collections.Generic;
using System.Threading.Tasks;

namespace ErsatzTV.Core.Health;

public interface IHealthCheckService
{
    Task<List<HealthCheckResult>> PerformHealthChecks();
}