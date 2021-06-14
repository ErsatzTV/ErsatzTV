using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Watermarks.Queries
{
    public record GetAllWatermarks : IRequest<List<WatermarkViewModel>>;
}
