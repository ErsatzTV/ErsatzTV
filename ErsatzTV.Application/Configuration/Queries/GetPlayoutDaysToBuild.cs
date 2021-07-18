using MediatR;

namespace ErsatzTV.Application.Configuration.Queries
{
    public record GetPlayoutDaysToBuild : IRequest<int>;
}
