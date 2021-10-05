using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record UpdateSmartCollection(int Id, string Query) : IRequest<Either<BaseError, Unit>>;
}
