using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core.Health;
using MediatR;

namespace ErsatzTV.Application.Health.Queries
{
    public class GetAllHealthCheckResultsHandler : IRequestHandler<GetAllHealthCheckResults, List<HealthCheckResult>>
    {
        private readonly IHealthCheckService _healthCheckService;

        public GetAllHealthCheckResultsHandler(IHealthCheckService healthCheckService) =>
            _healthCheckService = healthCheckService;

        public async Task<List<HealthCheckResult>> Handle(
            GetAllHealthCheckResults request,
            CancellationToken cancellationToken)
        {
            List<HealthCheckResult> results = await _healthCheckService.PerformHealthChecks();
            return results.Filter(r => r.Status != HealthCheckStatus.NotApplicable).ToList();
        }
    }
}
