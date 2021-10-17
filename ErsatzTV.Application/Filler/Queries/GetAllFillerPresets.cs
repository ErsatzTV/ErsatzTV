using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Filler.Queries
{
    public record GetAllFillerPresets : IRequest<List<FillerPresetViewModel>>;
}
