using MediatR;

namespace ErsatzTV.Application.HDHR.Queries
{
    public record GetHDHRTunerCount : IRequest<int>;
}
