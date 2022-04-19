using ErsatzTV.Core.Health;

namespace ErsatzTV.Application.Health;

public class GetAllHealthCheckResultsHandler : IRequestHandler<GetAllHealthCheckResults, List<HealthCheckResult>>
{
    private readonly IHealthCheckService _healthCheckService;

    public GetAllHealthCheckResultsHandler(IHealthCheckService healthCheckService) =>
        _healthCheckService = healthCheckService;

    public async Task<List<HealthCheckResult>> Handle(
        GetAllHealthCheckResults request,
        CancellationToken cancellationToken)
    {
        List<HealthCheckResult> results = await _healthCheckService.PerformHealthChecks(cancellationToken);
        return results.Filter(r => r.Status != HealthCheckStatus.NotApplicable).ToList();
    }
}
