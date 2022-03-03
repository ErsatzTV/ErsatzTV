using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Watermarks;

public record GetAllWatermarks : IRequest<List<WatermarkViewModel>>;