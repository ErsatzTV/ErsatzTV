using MediatR;

namespace ErsatzTV.Application.Configuration;

public record GetPlayoutDaysToBuild : IRequest<int>;