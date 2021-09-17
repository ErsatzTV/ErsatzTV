using System.Collections.Generic;
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

        public Task<List<HealthCheckResult>> Handle(
            GetAllHealthCheckResults request,
            CancellationToken cancellationToken) =>
            _healthCheckService.PerformHealthChecks();
    }
}
