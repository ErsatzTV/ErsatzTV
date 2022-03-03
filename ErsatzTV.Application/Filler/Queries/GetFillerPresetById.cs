using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Filler;

public record GetFillerPresetById(int Id) : IRequest<Option<FillerPresetViewModel>>;