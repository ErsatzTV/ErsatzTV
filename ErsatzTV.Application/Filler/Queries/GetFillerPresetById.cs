using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Filler.Queries
{
    public record GetFillerPresetById(int Id) : IRequest<Option<FillerPresetViewModel>>;
}
