using MediatR;

namespace ErsatzTV.Application.Configuration.Queries
{
    public record GetLibraryRefreshInterval : IRequest<int>;
}
