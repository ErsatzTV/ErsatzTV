using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Resolutions;

public record GetAllResolutions : IRequest<List<ResolutionViewModel>>;