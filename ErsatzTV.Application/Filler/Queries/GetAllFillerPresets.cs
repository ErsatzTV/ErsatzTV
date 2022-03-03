using System.Collections.Generic;
using MediatR;

namespace ErsatzTV.Application.Filler;

public record GetAllFillerPresets : IRequest<List<FillerPresetViewModel>>;