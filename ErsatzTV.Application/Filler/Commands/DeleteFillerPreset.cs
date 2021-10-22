using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Filler.Commands
{
    public record DeleteFillerPreset(int FillerPresetId) : IRequest<Either<BaseError, LanguageExt.Unit>>;
}
