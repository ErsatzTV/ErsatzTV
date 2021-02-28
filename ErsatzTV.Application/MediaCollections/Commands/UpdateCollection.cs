using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record UpdateCollection
        (int CollectionId, string Name) : MediatR.IRequest<Either<BaseError, Unit>>;
}
