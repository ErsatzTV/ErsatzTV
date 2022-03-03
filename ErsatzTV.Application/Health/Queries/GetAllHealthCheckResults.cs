using ErsatzTV.Core.Health;

namespace ErsatzTV.Application.Health;

public record GetAllHealthCheckResults : IRequest<List<HealthCheckResult>>;