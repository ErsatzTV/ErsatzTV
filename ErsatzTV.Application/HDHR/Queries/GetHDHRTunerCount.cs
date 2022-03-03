using MediatR;

namespace ErsatzTV.Application.HDHR;

public record GetHDHRTunerCount : IRequest<int>;