using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record UpdateSimpleMediaCollection
        (int MediaCollectionId, string Name) : MediatR.IRequest<Either<BaseError, Unit>>;
}
