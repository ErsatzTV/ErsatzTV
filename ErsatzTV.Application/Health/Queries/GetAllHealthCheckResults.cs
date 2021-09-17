using System.Collections.Generic;
using ErsatzTV.Core.Health;
using MediatR;

namespace ErsatzTV.Application.Health.Queries
{
    public record GetAllHealthCheckResults : IRequest<List<HealthCheckResult>>;
}
